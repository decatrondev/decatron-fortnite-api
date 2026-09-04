namespace Fortnite.Core.Ingest;

/// <summary>
/// Rutas de salida del ingest para un parche concreto. Todo cuelga de StagingRoot/&lt;patch&gt;/.
/// </summary>
public sealed class StagingLayout
{
    public StagingLayout(string stagingRoot, string patchVersion)
    {
        PatchDirectory = Path.Combine(stagingRoot, patchVersion);
        RawDirectory = Path.Combine(PatchDirectory, "raw");
        TexturesDirectory = Path.Combine(PatchDirectory, "textures");
        FileIndexPath = Path.Combine(PatchDirectory, "file-index.txt");
        CandidatesPath = Path.Combine(PatchDirectory, "candidates.txt");
        LogPath = Path.Combine(PatchDirectory, "ingest.log");
    }

    /// <summary>staging/&lt;patch&gt;/</summary>
    public string PatchDirectory { get; }

    /// <summary>staging/&lt;patch&gt;/raw/ — un JSON RawSprite por variante.</summary>
    public string RawDirectory { get; }

    /// <summary>staging/&lt;patch&gt;/textures/ — texturas exportadas sin normalizar.</summary>
    public string TexturesDirectory { get; }

    /// <summary>staging/&lt;patch&gt;/file-index.txt — lista completa de rutas del provider.</summary>
    public string FileIndexPath { get; }

    /// <summary>staging/&lt;patch&gt;/candidates.txt — subconjunto que matchea las pistas de sprite.</summary>
    public string CandidatesPath { get; }

    /// <summary>staging/&lt;patch&gt;/ingest.log</summary>
    public string LogPath { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(PatchDirectory);
        Directory.CreateDirectory(RawDirectory);
        Directory.CreateDirectory(TexturesDirectory);
    }
}
