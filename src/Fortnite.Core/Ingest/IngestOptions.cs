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

    /// <summary>
    /// Claves AES dinámicas por pakchunk (GUID + clave). Opcional: cubren chunks concretos
    /// que no usan la clave principal. Los sprites de coleccionables normalmente no las necesitan.
    /// </summary>
    public IReadOnlyList<DynamicAesKey> DynamicKeys { get; init; } = [];

    /// <summary>
    /// Nombre comercial de la temporada por plugin. La clave es el nombre del plugin GameFeature.
    /// Editá acá si el juego suma una temporada nueva.
    /// </summary>
    public IReadOnlyDictionary<string, string> SeasonNames { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["SpriteLibrary_CH7S3"] = "Runners",
        ["SpriteLibrary_Ch7S4"] = "Override",
    };

    /// <summary>Número de parche que identifica esta corrida. Ej: "34.20".</summary>
    public string PatchVersion { get; init; } = "";

    /// <summary>Ruta a un archivo .usmap para tipar los assets. Muy recomendado. Opcional.</summary>
    public string? MappingsFile { get; init; }

    /// <summary>Raíz donde se vuelca todo lo extraído sin procesar. Por defecto ./staging.</summary>
    public string StagingRoot { get; init; } = "staging";

    /// <summary>
    /// Raíz de la salida final ya procesada: catalog.json + sprites/&lt;id&gt;.png.
    /// Es lo que sirve la API/Nginx. Por defecto ./data.
    /// </summary>
    public string DataRoot { get; init; } = "data";

    /// <summary>
    /// Carpetas de iconos de sprites de coleccionable, confirmadas en el parche 42.10.
    /// Una por plugin de temporada (SpriteLibrary_*).
    /// </summary>
    public IReadOnlyList<string> SearchPaths { get; init; } =
    [
        "FortniteGame/Plugins/GameFeatures/SpriteLibrary_CH7S3/Content/UI",
        "FortniteGame/Plugins/GameFeatures/SpriteLibrary_Ch7S4/Content/UI",
    ];

    /// <summary>
    /// Subcadenas (case-insensitive) que marcan un package como candidato a sprite.
    /// </summary>
    public IReadOnlyList<string> CandidateHints { get; init; } =
    [
        "T_Icon_BR_Creature_Sprite_",
    ];

    /// <summary>Si es true, sólo vuelca el índice de archivos y candidatos; no exporta texturas.</summary>
    public bool DiscoveryOnly { get; init; } = true;

    /// <summary>
    /// Si viene una ruta de objeto (ej. "SpriteLibrary_CH7S3/Content/SpriteDefinitions/AirSprite/ESD_AirSprite"),
    /// el ingest sólo serializa ese asset a JSON y termina. Herramienta de diagnóstico de esquema.
    /// </summary>
    public string? DumpAsset { get; init; }

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

/// <summary>Clave AES dinámica: identifica un pak por su GUID y aporta su clave.</summary>
public sealed record DynamicAesKey
{
    /// <summary>GUID del pak en hex de 32 dígitos. Ej: "88FDEE76FF019E21306BBDC9E8E10A9D".</summary>
    public string Guid { get; init; } = "";

    /// <summary>Clave AES en hex; se acepta con o sin prefijo 0x.</summary>
    public string Key { get; init; } = "";
}
