using System.Diagnostics;
using System.Text.RegularExpressions;
using Fortnite.Core.Models;
using Fortnite.Persistence;

namespace Fortnite.Api;

/// <summary>
/// Sincronización manual (nunca automática) contra fortnite.gg/sprites: compara qué sprites
/// están confirmados como liberados ahí contra nuestro catálogo, para asistir la corrección
/// manual de "unreleased" desde /admin. Fortnite.gg no expone un flag "unreleased" — un sprite
/// que no aparece en su lista se interpreta como todavía no confirmado.
/// </summary>
public static partial class FortniteGgSync
{
    public sealed record GgCard(string Character, string Theme, string Season);

    public sealed record SyncResult(
        IReadOnlyList<SyncEntry> ToRelease,
        IReadOnlyList<SyncEntry> Suspicious,
        IReadOnlyList<GgCard> Missing);

    public sealed record SyncEntry(string Id, string Character, string Theme, string Season);

    private static readonly IReadOnlyDictionary<string, string> SeasonBySiteId = new Dictionary<string, string>
    {
        ["41"] = "Runners",
        ["42"] = "Override",
    };

    private static readonly IReadOnlyDictionary<string, string> ThemeByVariant = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["base"] = SpriteThemes.Basic,
        ["gold"] = SpriteThemes.Gold,
        ["candy"] = SpriteThemes.Candy,
        ["galaxy"] = SpriteThemes.Galaxy,
        ["gem"] = SpriteThemes.Gem,
        ["holofoil"] = SpriteThemes.Holofoil,
        ["cube"] = SpriteThemes.RiftCube,
        ["quack"] = SpriteThemes.Quack,
        ["hacker"] = "Hacker",
        ["cheatmaster"] = SpriteThemes.Cheat,
    };

    /// <summary>
    /// fortnite.gg nombra algunos personajes distinto a como los tenemos nosotros. Mapea el
    /// nombre de gg -> el nuestro antes de comparar. Se agrega una línea acá cada vez que
    /// aparezca un mismatch de nombre nuevo en el reporte (columna "missing").
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> CharacterAliasFromGg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Peeky Peely"] = "Peely",
    };

    // La clase puede ser "sprite-card" sola o "sprite-card is-hidden" (temporadas que no son
    // la activa por defecto en la web, pero igual vienen en el HTML server-rendered).
    [GeneratedRegex(@"<div class='sprite-card[^']*'(?<attrs>[^>]*)>", RegexOptions.Compiled)]
    private static partial Regex CardRegex();

    [GeneratedRegex(@"data-parent-name='(?<v>[^']*)'", RegexOptions.Compiled)]
    private static partial Regex ParentNameRegex();

    [GeneratedRegex(@"data-variant='(?<v>[^']*)'", RegexOptions.Compiled)]
    private static partial Regex VariantRegex();

    [GeneratedRegex(@"data-season='(?<v>[^']*)'", RegexOptions.Compiled)]
    private static partial Regex SeasonRegex();

    [GeneratedRegex(@"data-unreleased='(?<v>[01])'", RegexOptions.Compiled)]
    private static partial Regex UnreleasedRegex();

    /// <summary>
    /// Descarga fortnite.gg/sprites y parsea las cards con season/variant conocidos.
    /// Usa "curl" como subproceso en vez de HttpClient: Cloudflare bloquea el fingerprint TLS
    /// de .NET (403) pero acepta el de curl, aun con los mismos headers. Requiere curl en PATH
    /// (viene preinstalado en Ubuntu/Windows modernos).
    /// </summary>
    public static async Task<IReadOnlyList<GgCard>> FetchAsync(CancellationToken ct)
    {
        var html = await RunCurlAsync("https://fortnite.gg/sprites", ct);
        var cards = new List<GgCard>();

        foreach (Match m in CardRegex().Matches(html))
        {
            var attrs = m.Groups["attrs"].Value;

            var parent = ParentNameRegex().Match(attrs);
            var variant = VariantRegex().Match(attrs);
            var season = SeasonRegex().Match(attrs);

            if (!parent.Success || !variant.Success || !season.Success)
            {
                continue;
            }

            if (!ThemeByVariant.TryGetValue(variant.Groups["v"].Value, out var theme) ||
                !SeasonBySiteId.TryGetValue(season.Groups["v"].Value, out var seasonName))
            {
                continue;
            }

            // gg marca explícitamente algunas cards como todavía no liberadas (personajes recién
            // agregados, previsualizados pero sin confirmar). Esas no cuentan como "released" acá.
            var unreleased = UnreleasedRegex().Match(attrs);
            if (unreleased.Success && unreleased.Groups["v"].Value == "1")
            {
                continue;
            }

            var character = parent.Groups["v"].Value;
            if (CharacterAliasFromGg.TryGetValue(character, out var alias))
            {
                character = alias;
            }

            cards.Add(new GgCard(character, theme, seasonName));
        }

        return cards;
    }

    /// <summary>
    /// En Windows hay normalmente dos "curl.exe" en PATH: el nativo de System32 (motor TLS
    /// Schannel, mismo fingerprint bloqueado que HttpClient) y el de Git for Windows (funciona).
    /// En Linux (VPS) hay uno solo, sin ambigüedad. Evitamos System32 cuando hay alternativa.
    /// </summary>
    private static string ResolveCurlPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "curl";
        }

        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";

        foreach (var dir in path.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (dir.TrimEnd('\\').Equals(system32.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var candidate = Path.Combine(dir, "curl.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return "curl";
    }

    private static async Task<string> RunCurlAsync(string url, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ResolveCurlPath(),
            ArgumentList =
            {
                "-s", "-A",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36",
                url,
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("No se pudo iniciar curl.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"curl salió con código {process.ExitCode}: {await stderrTask}");
        }

        return await stdoutTask;
    }

    /// <summary>Compara las cards de gg contra el catálogo propio (por personaje+theme).</summary>
    public static SyncResult Compare(IReadOnlyList<GgCard> ggCards, IReadOnlyList<SpriteDatabase.AdminSpriteRow> catalog)
    {
        var index = new Dictionary<string, SpriteDatabase.AdminSpriteRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in catalog)
        {
            index[Key(s.Character ?? s.Name, s.Theme)] = s;
        }

        var ggKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toRelease = new List<SyncEntry>();
        var missing = new List<GgCard>();

        foreach (var card in ggCards)
        {
            var key = Key(card.Character, card.Theme);
            ggKeys.Add(key);

            if (!index.TryGetValue(key, out var sprite))
            {
                missing.Add(card);
                continue;
            }

            if (sprite.Unreleased)
            {
                toRelease.Add(new SyncEntry(sprite.Id, card.Character, card.Theme, card.Season));
            }
        }

        var suspicious = catalog
            .Where(s => !s.Unreleased && !ggKeys.Contains(Key(s.Character ?? s.Name, s.Theme)))
            .Select(s => new SyncEntry(s.Id, s.Character ?? s.Name, s.Theme, s.Season))
            .ToList();

        return new SyncResult(toRelease, suspicious, missing);
    }

    private static string Key(string character, string theme) => $"{character.Trim()}|{theme.Trim()}";
}
