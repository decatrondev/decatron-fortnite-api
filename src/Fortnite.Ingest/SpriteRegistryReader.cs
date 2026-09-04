using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using Fortnite.Core.Ingest;
using Newtonsoft.Json;

namespace Fortnite.Ingest;

/// <summary>
/// Vuelca a JSON las DataTables de sprites (DT_SpriteAssetRegistry, DT_SpriteGenericAssets, etc.)
/// para poder ver el esquema real y, en una pasada posterior, mapear rarity/unreleased/nombre.
/// Con .usmap presente el volcado sale tipado; sin él, sale lo que CUE4Parse pueda inferir.
/// </summary>
public static class SpriteRegistryReader
{
    private static readonly string[] TableNames =
    [
        "DT_SpriteAssetRegistry",
        "DT_SpriteGenericAssets",
        "DT_VariantWeights",
        "DT_VariantLootWeightTables",
    ];

    public static void Dump(FortniteFileProvider fp, StagingLayout layout, Action<string> log)
    {
        var provider = fp.Provider;
        var outDir = Path.Combine(layout.PatchDirectory, "registry");
        Directory.CreateDirectory(outDir);

        var matches = provider.Files
            .Where(kv => kv.Key.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase))
            .Where(kv => TableNames.Any(t => kv.Key.Contains("/" + t + ".", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (matches.Length == 0)
        {
            log("No se encontraron DataTables de sprites.");
            return;
        }

        foreach (var (path, file) in matches.Select(kv => (kv.Key, kv.Value)))
        {
            try
            {
                var package = provider.LoadPackage(file);
                var exports = package.GetExports().ToArray();

                var jsonPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(path) + ".json");
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(exports, Formatting.Indented));
                log($"  volcada: {Path.GetFileName(jsonPath)}");

                foreach (var table in exports.OfType<UDataTable>())
                {
                    var rows = table.RowMap;
                    var propNames = rows.Values
                        .SelectMany(r => r.Properties.Select(p => p.Name.Text))
                        .Distinct()
                        .OrderBy(n => n)
                        .ToArray();

                    log($"    {Path.GetFileNameWithoutExtension(path)}: {rows.Count} filas; columnas: {string.Join(", ", propNames)}");
                    log($"    primeras filas: {string.Join(", ", rows.Keys.Take(8).Select(k => k.Text))}");
                }
            }
            catch (Exception ex)
            {
                log($"  ERROR {Path.GetFileName(path)}: {ex.Message}");
            }
        }
    }
}
