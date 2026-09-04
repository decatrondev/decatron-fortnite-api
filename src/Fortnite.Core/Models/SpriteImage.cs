namespace Fortnite.Core.Models;

/// <summary>
/// Contrato de la imagen de cada sprite.
/// Nota: para los sprites de coleccionable, la resolución máxima que trae Fortnite es 128×128;
/// se sirven en su tamaño nativo (sin upscalear). El campo Width/Height es la referencia
/// de la spec original, no una garantía por-archivo.
/// </summary>
public static class SpriteImage
{
    public const int Width = 512;
    public const int Height = 512;

    /// <summary>Resolución nativa real de los iconos de sprite en los archivos del juego.</summary>
    public const int NativeSpriteSize = 128;

    /// <summary>PNG con canal alfa (fondo transparente). Nunca JPG.</summary>
    public const string Format = "png";

    /// <summary>Nombre de archivo para un id dado. Ej: "stormking_gold" -> "stormking_gold.png".</summary>
    public static string FileName(string id) => $"{id}.{Format}";

    /// <summary>Ruta pública relativa. Ej: "/sprites/stormking_gold.png".</summary>
    public static string PublicPath(string id) => $"/sprites/{FileName(id)}";
}
