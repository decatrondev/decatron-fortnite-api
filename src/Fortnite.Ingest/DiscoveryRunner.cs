using Fortnite.Core.Ingest;

namespace Fortnite.Ingest;

/// <summary>
/// Vuelca el índice completo de archivos montados y un subconjunto de candidatos a sprite.
/// Es el primer paso de la Fase 2: confirma que la clave AES funciona y revela las rutas
/// internas reales para afinar IngestOptions.SearchPaths.
/// </summary>
public static class DiscoveryRunner
{
    public static void Run(FortniteFileProvider fp, IngestOptions options, StagingLayout layout, Action<string> log)
    {
        var paths = fp.Provider.Files.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        File.WriteAllLines(layout.FileIndexPath, paths);
        log($"Índice completo: {paths.Length} rutas -> {layout.FileIndexPath}");

        bool IsCandidate(string path) =>
            options.SearchPaths.Any(sp => path.StartsWith(sp, StringComparison.OrdinalIgnoreCase)) ||
            options.CandidateHints.Any(h => path.Contains(h, StringComparison.OrdinalIgnoreCase));

        var candidates = paths.Where(IsCandidate).ToArray();
        File.WriteAllLines(layout.CandidatesPath, candidates);
        log($"Candidatos a sprite: {candidates.Length} -> {layout.CandidatesPath}");

        foreach (var group in candidates
                     .Select(p => p.Contains('/') ? p[..p.LastIndexOf('/')] : p)
                     .GroupBy(dir => dir)
                     .OrderByDescending(g => g.Count())
                     .Take(15))
        {
            log($"  {group.Count(),5}  {group.Key}");
        }
    }
}
