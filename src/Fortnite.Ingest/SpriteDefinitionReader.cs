using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Textures;
using Fortnite.Core.Ingest;
using Fortnite.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fortnite.Ingest;

/// <summary>
/// Fase 2c: recorre los assets ESD_* (ExtractableItemDefinition), que son la fuente de verdad
/// de cada variante de sprite: nombre, rareza, theme, icono y número de colección.
/// Por cada uno resuelve el icono, lo exporta a PNG y arma el objeto Sprite de la spec.
/// </summary>
public static class SpriteDefinitionReader
{
    public sealed record Result(IReadOnlyList<Sprite> Catalog, IReadOnlyList<RawSprite> Raw, IReadOnlyList<string> Warnings);

    public static Result Run(FortniteFileProvider fp, IngestOptions options, StagingLayout layout, Action<string> log)
    {
        var provider = fp.Provider;
        var warnings = new List<string>();

        var esdFiles = provider.Files
            .Where(kv => kv.Key.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase))
            .Where(kv => kv.Key.Contains("/SpriteDefinitions/", StringComparison.OrdinalIgnoreCase))
            .Where(kv => Path.GetFileName(kv.Key).StartsWith("ESD_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        log($"Definiciones ESD encontradas: {esdFiles.Length}");

        // Primera pasada: leer props crudas de cada ESD.
        var parsed = new List<EsdRecord>();
        foreach (var (path, file) in esdFiles.Select(kv => (kv.Key, kv.Value)))
        {
            try
            {
                var exports = provider.LoadPackage(file).GetExports();
                var arr = JArray.Parse(JsonConvert.SerializeObject(exports));
                var root = arr.OfType<JObject>().FirstOrDefault();
                if (root is null)
                {
                    continue;
                }

                var props = root["Properties"] as JObject ?? [];
                var dataList = props["DataList"] as JArray ?? [];

                string? rarity = null, iconPath = null, largeIconPath = null, variantTag = null;
                foreach (var d in dataList.OfType<JObject>())
                {
                    rarity ??= (string?)d["Rarity"];
                    iconPath ??= (string?)d["Icon"]?["AssetPathName"];
                    largeIconPath ??= (string?)d["LargeIcon"]?["AssetPathName"];
                    variantTag ??= (string?)d["VariantRarityTag"]?["TagName"];
                }

                parsed.Add(new EsdRecord
                {
                    AssetPath = path,
                    Plugin = PluginOf(path),
                    EsdName = Path.GetFileNameWithoutExtension(path),
                    ItemName = (string?)props["ItemName"]?["SourceString"],
                    Rarity = StripEnum(rarity),
                    VariantToken = VariantTokenFrom(variantTag, Path.GetFileNameWithoutExtension(path)),
                    IconPath = iconPath ?? largeIconPath,
                    DexNumber = (int?)props["DexNumber"],
                });
            }
            catch (Exception ex)
            {
                warnings.Add($"ESD ilegible {Path.GetFileName(path)}: {ex.Message}");
            }
        }

        // Índice de bases (sin _Variant_) para deducir personaje de las variantes.
        var baseByKey = parsed
            .Where(p => !p.EsdName.Contains("_Variant_", StringComparison.OrdinalIgnoreCase) &&
                        !p.EsdName.Contains("_Variation_", StringComparison.OrdinalIgnoreCase))
            .GroupBy(p => (p.Plugin, BaseStem(p.EsdName)))
            .ToDictionary(g => g.Key, g => g.First());

        var catalog = new List<Sprite>();
        var raw = new List<RawSprite>();

        var skippedArchetype = 0;

        foreach (var rec in parsed)
        {
            // "_Variant_A" -> VariantRarityTag "UseArchetype": es un placeholder que reusa la base,
            // no una variante cosmética. No va al catálogo.
            if (string.Equals(rec.VariantToken, "UseArchetype", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rec.VariantToken, "A", StringComparison.OrdinalIgnoreCase))
            {
                skippedArchetype++;
                continue;
            }

            var stem = BaseStem(rec.EsdName);
            baseByKey.TryGetValue((rec.Plugin, stem), out var baseRec);

            var character = CharacterName(baseRec?.ItemName ?? rec.ItemName ?? stem);
            var theme = ThemeCanonical(rec.VariantToken);
            var season = options.SeasonNames.TryGetValue(rec.Plugin, out var s) ? s : rec.Plugin;
            var id = SpriteId.From(character, theme);
            var name = (rec.ItemName ?? $"{theme} {character}")
                .Replace(" Sprite", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            string? textureFile = null;
            if (!string.IsNullOrWhiteSpace(rec.IconPath))
            {
                textureFile = TryExportIcon(provider, rec.IconPath!, id, layout, out var note);
                if (textureFile is null)
                {
                    warnings.Add($"{id}: icono no exportado ({note})");
                }
            }
            else
            {
                warnings.Add($"{id}: ESD sin Icon");
            }

            var rarityValue = rec.Rarity ?? "";
            if (rarityValue.Length > 0 && !SpriteRarities.IsKnown(rarityValue))
            {
                warnings.Add($"{id}: rareza fuera de la spec: '{rarityValue}'");
            }

            catalog.Add(new Sprite
            {
                Id = id,
                Name = name,
                Theme = theme,
                Rarity = rarityValue,
                Unreleased = false,
                Season = season,
                Character = character,
            });

            raw.Add(new RawSprite
            {
                SourceAssetPath = rec.AssetPath,
                TextureFile = textureFile,
                Character = character,
                Theme = theme,
                Rarity = rarityValue,
                Season = season,
                UnreleasedHint = false,
                Notes = $"ESD={rec.EsdName}; dex={rec.DexNumber?.ToString() ?? "-"}; variantToken={rec.VariantToken ?? "-"}",
            });
        }

        if (skippedArchetype > 0)
        {
            log($"Variantes 'UseArchetype' (placeholder de la base) omitidas: {skippedArchetype}");
        }

        log($"Sprites en catálogo: {catalog.Count}");
        log($"PNG exportados: {raw.Count(r => r.TextureFile is not null)} -> {layout.TexturesDirectory}");
        if (warnings.Count > 0)
        {
            log($"Avisos: {warnings.Count} (ver ingest.log)");
        }

        return new Result(catalog, raw, warnings);
    }

    private sealed record EsdRecord
    {
        public required string AssetPath { get; init; }
        public required string Plugin { get; init; }
        public required string EsdName { get; init; }
        public string? ItemName { get; init; }
        public string? Rarity { get; init; }
        public string? VariantToken { get; init; }
        public string? IconPath { get; init; }
        public int? DexNumber { get; init; }
    }

    private static string PluginOf(string path)
    {
        const string marker = "/GameFeatures/";
        var i = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0)
        {
            return "?";
        }

        var rest = path[(i + marker.Length)..];
        var slash = rest.IndexOf('/');
        return slash < 0 ? rest : rest[..slash];
    }

    /// <summary>"ESD_BossSprite_Variant_Gold" -> "ESD_BossSprite"; "ESD_8BitBlasterSprite" -> igual.</summary>
    private static string BaseStem(string esdName)
    {
        foreach (var sep in new[] { "_Variant_", "_Variation_" })
        {
            var i = esdName.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (i >= 0)
            {
                return esdName[..i];
            }
        }

        return esdName;
    }

    private static string? VariantTokenFrom(string? variantRarityTag, string esdName)
    {
        if (!string.IsNullOrWhiteSpace(variantRarityTag))
        {
            var seg = variantRarityTag.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (seg.Length > 0)
            {
                return seg[^1];
            }
        }

        foreach (var sep in new[] { "_Variant_", "_Variation_" })
        {
            var i = esdName.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (i >= 0)
            {
                return esdName[(i + sep.Length)..];
            }
        }

        return null; // base
    }

    private static readonly IReadOnlyDictionary<string, string> ThemeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Gold"] = SpriteThemes.Gold,
        ["Candy"] = SpriteThemes.Candy,
        ["Gummy"] = SpriteThemes.Candy,
        ["Galaxy"] = SpriteThemes.Galaxy,
        ["Gem"] = SpriteThemes.Gem,
        ["Holofoil"] = SpriteThemes.Holofoil,
        ["Holo"] = SpriteThemes.Holofoil,
        ["Cube"] = SpriteThemes.RiftCube,
        ["Rift"] = SpriteThemes.RiftCube,
        ["Cheat"] = SpriteThemes.Cheat,
        ["CheatMaster"] = SpriteThemes.Cheat,
        ["Quack"] = SpriteThemes.Quack,
    };

    /// <summary>Mapea a un theme de la spec; si no lo reconoce, devuelve el token crudo (decisión del usuario).</summary>
    private static string ThemeCanonical(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return SpriteThemes.Basic;
        }

        return ThemeMap.TryGetValue(token, out var canonical) ? canonical : token;
    }

    private static string CharacterName(string itemNameOrStem)
    {
        var n = itemNameOrStem;
        if (n.StartsWith("ESD_", StringComparison.OrdinalIgnoreCase))
        {
            n = n[4..];
            n = SpriteAssetName.HumanizeCharacter(n);
        }

        // "Boss Sprite" -> "Boss"; "Gold Boss Sprite" -> ya viene del base, pero por las dudas:
        n = n.Replace(" Sprite", "", StringComparison.OrdinalIgnoreCase).Trim();
        return n.Length == 0 ? itemNameOrStem : n;
    }

    private static string? StripEnum(string? v)
    {
        if (string.IsNullOrWhiteSpace(v))
        {
            return null;
        }

        var i = v.LastIndexOf("::", StringComparison.Ordinal);
        return i >= 0 ? v[(i + 2)..] : v;
    }

    /// <summary>
    /// "/SpriteLibrary_CH7S3/UI/T_Icon_x.T_Icon_x" -> exporta el PNG a textures/&lt;id&gt;.png.
    /// Devuelve el nombre de archivo, o null con motivo en 'note'.
    /// </summary>
    private static string? TryExportIcon(CUE4Parse.FileProvider.AbstractFileProvider provider, string iconObjectPath, string id, StagingLayout layout, out string note)
    {
        note = "";
        var objPath = iconObjectPath;
        var dot = objPath.IndexOf('.');
        if (dot >= 0)
        {
            objPath = objPath[..dot];
        }

        var trimmed = objPath.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        if (firstSlash < 0)
        {
            note = $"ruta rara: {iconObjectPath}";
            return null;
        }

        var plugin = trimmed[..firstSlash];
        var tail = trimmed[(firstSlash + 1)..];
        var key = $"FortniteGame/Plugins/GameFeatures/{plugin}/Content/{tail}.uasset";

        if (!provider.Files.ContainsKey(key))
        {
            var basename = tail.Split('/')[^1];
            var alt = provider.Files.Keys.FirstOrDefault(k =>
                k.EndsWith("/" + basename + ".uasset", StringComparison.OrdinalIgnoreCase));
            if (alt is null)
            {
                note = $"textura no encontrada: {key}";
                return null;
            }

            key = alt;
        }

        try
        {
            var texture = provider.LoadPackage(key).GetExports().OfType<UTexture2D>().FirstOrDefault();
            if (texture is null)
            {
                note = "sin UTexture2D";
                return null;
            }

            var decoded = texture.Decode(ETexturePlatform.DesktopMobile);
            if (decoded is null)
            {
                note = "decode nulo";
                return null;
            }

            var outFile = Path.Combine(layout.TexturesDirectory, id + ".png");
            File.WriteAllBytes(outFile, decoded.Encode(ETextureFormat.Png, saveHdrAsHdr: false, out _));
            return id + ".png";
        }
        catch (Exception ex)
        {
            note = ex.Message;
            return null;
        }
    }
}
