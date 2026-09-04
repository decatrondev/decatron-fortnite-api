using Fortnite.Core.Models;

namespace Fortnite.Core.Ingest;

/// <summary>
/// Parsea el nombre de una textura de icono de sprite y deduce personaje + theme.
/// Cubre los dos patrones vistos en el parche 42.10:
///   S3: T_Icon_BR_Creature_Sprite_&lt;Character&gt;[_&lt;Theme&gt;]_ui[_L]
///   S4: T_Icon_BR_Creature_Sprite_&lt;Character&gt;[_&lt;Theme&gt;][_L]
/// </summary>
public static class SpriteAssetName
{
    private const string Prefix = "T_Icon_BR_Creature_Sprite_";

    /// <summary>Tokens de theme del juego -> valor canónico de la spec.</summary>
    private static readonly IReadOnlyDictionary<string, string> ThemeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Gold"] = SpriteThemes.Gold,
        ["Candy"] = SpriteThemes.Candy,
        ["Galaxy"] = SpriteThemes.Galaxy,
        ["Gem"] = SpriteThemes.Gem,
        ["Holofoil"] = SpriteThemes.Holofoil,
        ["Holo"] = SpriteThemes.Holofoil,
        ["Cube"] = SpriteThemes.RiftCube,
        ["Rift"] = SpriteThemes.RiftCube,
        ["Cheat"] = SpriteThemes.Cheat,
        ["Cheatmaster"] = SpriteThemes.Cheat,
        ["Quack"] = SpriteThemes.Quack,
    };

    public sealed record Parsed
    {
        public required string Character { get; init; }
        public required string Theme { get; init; }
        public required bool IsLowRes { get; init; }

        /// <summary>true si el sufijo no es un theme conocido de la spec (ej. Hacker, LootHacker, Glasses).</summary>
        public required bool IsNonStandardVariant { get; init; }

        /// <summary>El token de variante crudo tal cual venía en el archivo (para diagnóstico).</summary>
        public string? RawVariantToken { get; init; }
    }

    /// <summary>Devuelve null si el nombre no corresponde a un icono de sprite de coleccionable.</summary>
    public static Parsed? TryParse(string assetNameOrPath)
    {
        var name = assetNameOrPath;
        var slash = name.LastIndexOf('/');
        if (slash >= 0)
        {
            name = name[(slash + 1)..];
        }

        var dot = name.IndexOf('.');
        if (dot >= 0)
        {
            name = name[..dot];
        }

        if (!name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rest = name[Prefix.Length..];

        var isLowRes = rest.EndsWith("_L", StringComparison.OrdinalIgnoreCase);
        if (isLowRes)
        {
            rest = rest[..^2];
        }

        if (rest.EndsWith("_ui", StringComparison.OrdinalIgnoreCase))
        {
            rest = rest[..^3];
        }

        rest = rest.Trim('_');
        if (rest.Length == 0)
        {
            return null;
        }

        var parts = rest.Split('_', StringSplitOptions.RemoveEmptyEntries);

        // Sin sufijo de variante -> theme base (Basic).
        if (parts.Length == 1)
        {
            return new Parsed
            {
                Character = parts[0],
                Theme = SpriteThemes.Basic,
                IsLowRes = isLowRes,
                IsNonStandardVariant = false,
            };
        }

        var last = parts[^1];
        if (ThemeMap.TryGetValue(last, out var canonical))
        {
            return new Parsed
            {
                Character = string.Join('_', parts[..^1]),
                Theme = canonical,
                IsLowRes = isLowRes,
                IsNonStandardVariant = false,
                RawVariantToken = last,
            };
        }

        // Sufijo desconocido: probablemente una skin especial del personaje, no un theme de la spec.
        return new Parsed
        {
            Character = string.Join('_', parts[..^1]),
            Theme = last,
            IsLowRes = isLowRes,
            IsNonStandardVariant = true,
            RawVariantToken = last,
        };
    }

    /// <summary>"StormScout" -> "Storm Scout"; "EightBitBlaster" -> "Eight Bit Blaster".</summary>
    public static string HumanizeCharacter(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase))
        {
            return pascalCase;
        }

        var sb = new System.Text.StringBuilder(pascalCase.Length + 8);
        for (var i = 0; i < pascalCase.Length; i++)
        {
            var c = pascalCase[i];
            if (i > 0 && char.IsUpper(c) && (!char.IsUpper(pascalCase[i - 1]) || (i + 1 < pascalCase.Length && char.IsLower(pascalCase[i + 1]))))
            {
                sb.Append(' ');
            }

            sb.Append(c == '_' ? ' ' : c);
        }

        return sb.ToString().Trim();
    }
}
