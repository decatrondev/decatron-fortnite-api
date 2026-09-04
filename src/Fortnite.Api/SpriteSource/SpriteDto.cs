namespace Fortnite.Api.SpriteSource;

/// <summary>Objeto de salida de la API. Coincide 1:1 con el formato de la spec.</summary>
public sealed record SpriteDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Theme { get; init; }
    public required string Rarity { get; init; }
    public required bool Unreleased { get; init; }
    public required string Season { get; init; }
    public string? Character { get; init; }
}
