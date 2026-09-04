namespace Fortnite.Core.Ingest;

/// <summary>
/// Parámetros de una corrida de ingest. Se cargan de appsettings.Local.json,
/// variables de entorno (prefijo INGEST_) o argumentos de línea de comandos.
/// </summary>
public sealed record IngestOptions
{
    /// <summary>Carpeta Paks de Fortnite. Ej: C:\...\Fortnite\FortniteGame\Content\Paks.</summary>
    public string PaksDirectory { get; init; } = "";

    /// <summary>Clave AES principal del parche, en hex con prefijo 0x. Vacío = sólo archivos sin cifrar.</summary>
    public string AesKey { get; init; } = "";

    /// <summary>Número de parche que identifica esta corrida. Ej: "34.20".</summary>
    public string PatchVersion { get; init; } = "";

    /// <summary>Ruta a un archivo .usmap para tipar los assets. Muy recomendado. Opcional.</summary>
    public string? MappingsFile { get; init; }

    /// <summary>Raíz donde se vuelca todo lo extraído. Por defecto ./staging.</summary>
    public string StagingRoot { get; init; } = "staging";

    /// <summary>
    /// Prefijos de ruta interna donde buscar assets de sprites de coleccionables.
    /// Se ajustan cuando la fase de descubrimiento revele las rutas reales.
    /// </summary>
    public IReadOnlyList<string> SearchPaths { get; init; } =
    [
        "FortniteGame/Content/Athena/Items/Cosmetics",
        "FortniteGame/Content/UI/Foundation",
    ];

    /// <summary>
    /// Subcadenas (case-insensitive) que marcan un package como candidato a sprite.
    /// También provisorio hasta el descubrimiento.
    /// </summary>
    public IReadOnlyList<string> CandidateHints { get; init; } =
    [
        "sticker", "card", "collectible", "sprite", "foil", "holo",
    ];

    /// <summary>Si es true, sólo vuelca el índice de archivos y candidatos; no exporta texturas.</summary>
    public bool DiscoveryOnly { get; init; } = true;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(PaksDirectory))
        {
            errors.Add("PaksDirectory es obligatorio.");
        }
        else if (!Directory.Exists(PaksDirectory))
        {
            errors.Add($"PaksDirectory no existe: {PaksDirectory}");
        }

        if (string.IsNullOrWhiteSpace(PatchVersion))
        {
            errors.Add("PatchVersion es obligatorio (ej. \"34.20\").");
        }

        if (!string.IsNullOrWhiteSpace(AesKey) &&
            !(AesKey.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && AesKey.Length == 66))
        {
            errors.Add("AesKey debe ser hex de 64 dígitos con prefijo 0x.");
        }

        if (!string.IsNullOrWhiteSpace(MappingsFile) && !File.Exists(MappingsFile))
        {
            errors.Add($"MappingsFile no existe: {MappingsFile}");
        }

        return errors;
    }
}
