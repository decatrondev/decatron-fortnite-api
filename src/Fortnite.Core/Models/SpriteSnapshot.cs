namespace Fortnite.Core.Models;

/// <summary>
/// Foto del catálogo completo de sprites tras un parche concreto.
/// Se guarda una por cada ingest para poder comparar parches y detectar
/// transiciones unreleased: true -> false.
/// </summary>
public sealed record SpriteSnapshot
{
    /// <summary>Versión del parche de Fortnite. Ej: "34.20".</summary>
    public required string PatchVersion { get; init; }

    /// <summary>Momento del ingest, en UTC.</summary>
    public required DateTimeOffset TakenAtUtc { get; init; }

    /// <summary>Catálogo completo capturado en ese ingest.</summary>
    public required IReadOnlyList<Sprite> Sprites { get; init; }
}
