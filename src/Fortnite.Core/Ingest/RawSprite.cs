namespace Fortnite.Core.Ingest;

/// <summary>
/// Lo que el ingest logra leer de un asset antes de normalizar. Campos opcionales:
/// lo que no se pueda deducir del asset queda en null y lo resuelve la fase de Processing
/// o una tabla de correcciones manual.
/// </summary>
public sealed record RawSprite
{
    /// <summary>Ruta interna del package de origen. Ej: "FortniteGame/Content/.../Foil_StormScout".</summary>
    public required string SourceAssetPath { get; init; }

    /// <summary>Ruta relativa dentro de staging/&lt;patch&gt;/textures del PNG crudo exportado. Null si aún no se exportó.</summary>
    public string? TextureFile { get; init; }

    public string? Character { get; init; }
    public string? Theme { get; init; }
    public string? Rarity { get; init; }
    public string? Season { get; init; }

    /// <summary>Pista de disponibilidad leída del asset (flag de "hidden"/"comingSoon"/fecha). Se confirma en Reconcile.</summary>
    public bool? UnreleasedHint { get; init; }

    /// <summary>Notas del ingest: qué se dedujo y de dónde, o por qué quedó incompleto.</summary>
    public string? Notes { get; init; }
}
