// Fortnite.Ingest — Fase 2.
// Monta los .pak de una instalación local de Fortnite y vuelca el índice de archivos
// + una lista de candidatos a sprite. La extracción de texturas llega en la Fase 2b,
// cuando candidates.txt confirme las rutas internas reales.
//
// Configuración (por prioridad): argumentos > env INGEST_* > appsettings.Local.json > appsettings.json
// Ejemplo:
//   dotnet run --project src/Fortnite.Ingest -- \
//     --Ingest:PaksDirectory "C:\...\Fortnite\FortniteGame\Content\Paks" \
//     --Ingest:AesKey 0x<64hex> --Ingest:PatchVersion 34.20

using Fortnite.Core.Ingest;
using Fortnite.Ingest;
using Microsoft.Extensions.Configuration;

// Base = carpeta del ejecutable (donde se copian los appsettings), no el cwd del shell.
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json"), optional: true)
    .AddEnvironmentVariables("INGEST_")
    .AddCommandLine(args)
    .Build();

var options = config.GetSection("Ingest").Get<IngestOptions>() ?? new IngestOptions();

var errors = options.Validate();
if (errors.Count > 0)
{
    Console.Error.WriteLine("Configuración inválida:");
    foreach (var e in errors)
    {
        Console.Error.WriteLine($"  - {e}");
    }

    return 1;
}

var layout = new StagingLayout(options.StagingRoot, options.PatchVersion);
layout.EnsureCreated();

await using var logFile = new StreamWriter(layout.LogPath, append: true) { AutoFlush = true };
void Log(string message)
{
    Console.WriteLine(message);
    logFile.WriteLine($"{DateTimeOffset.Now:o}  {message}");
}

Log($"Ingest parche {options.PatchVersion}");
Log($"Paks: {options.PaksDirectory}");
Log($"Mappings: {options.MappingsFile ?? "(ninguno)"}");

using var provider = new FortniteFileProvider(options);
try
{
    provider.Initialize();
}
catch (Exception ex)
{
    Log($"ERROR al montar los paks: {ex.Message}");
    Log("Revisá que la clave AES corresponda al parche instalado.");
    return 2;
}

Log($"Provider montado: {provider.Provider.Files.Count} archivos.");

if (!string.IsNullOrWhiteSpace(options.DumpAsset))
{
    var dumpDir = Path.Combine(layout.PatchDirectory, "dump");
    Directory.CreateDirectory(dumpDir);
    try
    {
        var pkg = provider.Provider.LoadPackage(options.DumpAsset);
        var outPath = Path.Combine(dumpDir, options.DumpAsset.Split('/', '\\')[^1] + ".json");
        File.WriteAllText(outPath, Newtonsoft.Json.JsonConvert.SerializeObject(pkg.GetExports(), Newtonsoft.Json.Formatting.Indented));
        Log($"Asset volcado -> {outPath}");
        return 0;
    }
    catch (Exception ex)
    {
        Log($"ERROR al volcar {options.DumpAsset}: {ex.Message}");
        return 3;
    }
}

DiscoveryRunner.Run(provider, options, layout, Log);

if (options.DiscoveryOnly)
{
    Log("DiscoveryOnly = true. Revisá candidates.txt, ajustá Ingest:SearchPaths y desactivá DiscoveryOnly para extraer.");
    return 0;
}

Log("--- Fase 2c: lectura de definiciones ESD ---");
var jsonOpts = new System.Text.Json.JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
};
var result = SpriteDefinitionReader.Run(provider, options, layout, Log);

foreach (var raw in result.Raw)
{
    var id = Path.GetFileNameWithoutExtension(raw.TextureFile ?? Fortnite.Core.Models.SpriteId.From(raw.Character ?? "x", raw.Theme ?? "x"));
    File.WriteAllText(Path.Combine(layout.RawDirectory, id + ".json"),
        System.Text.Json.JsonSerializer.Serialize(raw, jsonOpts));
}

var catalogPath = Path.Combine(layout.PatchDirectory, "catalog.json");
File.WriteAllText(catalogPath, System.Text.Json.JsonSerializer.Serialize(result.Catalog, jsonOpts));
Log($"Catálogo -> {catalogPath} ({result.Catalog.Count} sprites)");

if (result.Warnings.Count > 0)
{
    foreach (var w in result.Warnings)
    {
        logFile.WriteLine($"{DateTimeOffset.Now:o}  AVISO  {w}");
    }
}

Log("--- volcado de DataTables (referencia) ---");
SpriteRegistryReader.Dump(provider, layout, Log);

Log("Listo. Revisá staging/<patch>/catalog.json, /textures/, /raw/ y /registry/.");
return 0;
