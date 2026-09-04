using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Textures;
using Fortnite.Core.Ingest;
using Fortnite.Core.Models;

namespace Fortnite.Ingest;

/// <summary>
/// Fase 2b: enumera las texturas de icono de sprite, elige full-res sobre low-res,
/// decodifica y exporta PNG a staging/&lt;patch&gt;/textures/&lt;id&gt;.png.
/// La metadata (rarity, unreleased, name) la completa SpriteRegistryReader.
/// </summary>
public static class SpriteTextureExtractor
{
    public static IReadOnlyList<RawSprite> Run(
        FortniteFileProvider fp, IngestOptions options, StagingLayout layout, Action<string> log)
    {
        var provider = fp.Provider;

        var candidates = provider.Files
            .Where(kv => kv.Key.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase))
            .Where(kv => kv.Key.Contains("T_Icon_BR_Creature_Sprite_", StringComparison.OrdinalIgnoreCase))
            .Where(kv => options.SearchPaths.Count == 0 ||
                         options.SearchPaths.Any(sp => kv.Key.StartsWith(sp, StringComparison.OrdinalIgnoreCase)))
            .Select(kv => (Path: kv.Key, File: kv.Value, Info: SpriteAssetName.TryParse(kv.Key)))
            .Where(x => x.Info is not null)
            .ToArray();

        log($"Texturas candidatas: {candidates.Length}");

        // Una entrada por (personaje, theme): preferimos la que NO es _L.
        var chosen = candidates
            .GroupBy(x => (
                x.Info!.Character.ToLowerInvariant(),
                x.Info.Theme.ToLowerInvariant(),
                x.Info.IsNonStandardVariant))
            .Select(g => g.OrderBy(x => x.Info!.IsLowRes ? 1 : 0).First())
            .ToArray();

        var results = new List<RawSprite>();
        var skippedNonStandard = 0;

        foreach (var (path, file, info) in chosen)
        {
            if (info!.IsNonStandardVariant)
            {
                skippedNonStandard++;
                continue;
            }

            try
            {
                var package = provider.LoadPackage(file);
                var texture = package.GetExports().OfType<UTexture2D>().FirstOrDefault();
                if (texture is null)
                {
                    log($"  sin UTexture2D: {path}");
                    continue;
                }

                var decoded = texture.Decode(ETexturePlatform.DesktopMobile);
                if (decoded is null)
                {
                    log($"  decode nulo: {path}");
                    continue;
                }

                var character = SpriteAssetName.HumanizeCharacter(info.Character);
                var id = SpriteId.From(character, info.Theme);
                var outFile = Path.Combine(layout.TexturesDirectory, id + ".png");

                var png = decoded.Encode(ETextureFormat.Png, saveHdrAsHdr: false, out _);
                File.WriteAllBytes(outFile, png);

                results.Add(new RawSprite
                {
                    SourceAssetPath = path,
                    TextureFile = Path.GetFileName(outFile),
                    Character = character,
                    Theme = info.Theme,
                    Season = SeasonFromPath(path),
                    Rarity = null,
                    UnreleasedHint = null,
                    Notes = $"{decoded.Width}x{decoded.Height}; theme deducido del nombre de archivo" +
                            (info.IsLowRes ? "; sólo había _L (baja resolución)" : ""),
                });
            }
            catch (Exception ex)
            {
                log($"  ERROR {Path.GetFileName(path)}: {ex.Message}");
            }
        }

        if (skippedNonStandard > 0)
        {
            log($"Variantes no estándar omitidas (no son themes de la spec): {skippedNonStandard}");
        }

        log($"PNG exportados: {results.Count} -> {layout.TexturesDirectory}");
        return results;
    }

    private static string SeasonFromPath(string path)
    {
        if (path.Contains("SpriteLibrary_CH7S3", StringComparison.OrdinalIgnoreCase))
        {
            return "Ch7 S3";
        }

        if (path.Contains("SpriteLibrary_Ch7S4", StringComparison.OrdinalIgnoreCase))
        {
            return "Override";
        }

        var i = path.IndexOf("SpriteLibrary_", StringComparison.OrdinalIgnoreCase);
        return i >= 0 ? path[(i + "SpriteLibrary_".Length)..].Split('/')[0] : "?";
    }
}
