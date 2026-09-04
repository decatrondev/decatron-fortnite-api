namespace Fortnite.Core.Models;

/// <summary>
/// Contrato fijo de la imagen de cada sprite. Todas las imágenes cumplen esto sin excepción.
/// </summary>
public static class SpriteImage
{
    public const int Width = 512;
    public const int Height = 512;

    /// <summary>PNG con canal alfa (fondo transparente). Nunca JPG.</summary>
    public const string Format = "png";

    /// <summary>Nombre de archivo para un id dado. Ej: "stormking_gold" -> "stormking_gold.png".</summary>
    public static string FileName(string id) => $"{id}.{Format}";

    /// <summary>Ruta pública relativa. Ej: "/sprites/stormking_gold.png".</summary>
    public static string PublicPath(string id) => $"/sprites/{FileName(id)}";
}
