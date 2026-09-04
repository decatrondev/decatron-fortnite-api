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

DiscoveryRunner.Run(provider, options, layout, Log);

if (options.DiscoveryOnly)
{
    Log("DiscoveryOnly = true. Revisá candidates.txt, ajustá Ingest:SearchPaths y desactivá DiscoveryOnly para extraer.");
    return 0;
}

Log("--- Fase 2b: extracción de texturas ---");
var rawSprites = SpriteTextureExtractor.Run(provider, options, layout, Log);

foreach (var raw in rawSprites)
{
    var id = Path.GetFileNameWithoutExtension(raw.TextureFile!);
    var rawPath = Path.Combine(layout.RawDirectory, id + ".json");
    File.WriteAllText(rawPath, System.Text.Json.JsonSerializer.Serialize(raw, new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
    }));
}

Log($"RawSprite volcados: {rawSprites.Count} -> {layout.RawDirectory}");

Log("--- Fase 2b: volcado de DataTables ---");
SpriteRegistryReader.Dump(provider, layout, Log);

Log("Listo. Revisá staging/<patch>/textures/, /raw/ y /registry/.");
return 0;
