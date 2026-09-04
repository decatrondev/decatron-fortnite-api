namespace Fortnite.Core.Models;

/// <summary>
/// Filtros para listar sprites. Todos opcionales; null = sin filtrar por ese campo.
/// </summary>
public sealed record SpriteQuery
{
    public string? Season { get; init; }
    public string? Theme { get; init; }
    public string? Rarity { get; init; }
    public bool? Unreleased { get; init; }
    public string? Character { get; init; }

    public bool Matches(Sprite s) =>
        (Season is null || string.Equals(s.Season, Season, StringComparison.OrdinalIgnoreCase)) &&
        (Theme is null || string.Equals(s.Theme, Theme, StringComparison.OrdinalIgnoreCase)) &&
        (Rarity is null || string.Equals(s.Rarity, Rarity, StringComparison.OrdinalIgnoreCase)) &&
        (Unreleased is null || s.Unreleased == Unreleased) &&
        (Character is null || string.Equals(s.Character, Character, StringComparison.OrdinalIgnoreCase));
}
