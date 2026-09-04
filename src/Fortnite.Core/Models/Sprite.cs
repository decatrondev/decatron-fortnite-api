namespace Fortnite.Core.Models;

/// <summary>
/// Un sprite de coleccionable de Fortnite, en una variante (theme) concreta.
/// Los campos coinciden 1:1 con el formato que consumen los scripts de sync,
/// por eso son strings planos y no enums: el consumidor lee sin transformar nada.
/// </summary>
public sealed record Sprite
{
    /// <summary>Clave única. Formato: personaje_tema en minúsculas y guion bajo. Ej: "stormking_gold".</summary>
    public required string Id { get; init; }

    /// <summary>Nombre para mostrar. Ej: "Gold Storm Scout".</summary>
    public required string Name { get; init; }

    /// <summary>Variante del sprite. Ver <see cref="SpriteThemes"/>. Ej: "Gold".</summary>
    public required string Theme { get; init; }

    /// <summary>Rareza. Ver <see cref="SpriteRarities"/>. Ej: "Special".</summary>
    public required string Rarity { get; init; }

    /// <summary>
    /// El campo más importante: indica si el sprite ya se puede conseguir en el juego (false)
    /// o si existe en los archivos pero todavía no está disponible (true).
    /// </summary>
    public required bool Unreleased { get; init; }

    /// <summary>Nombre de la temporada. Ej: "Override".</summary>
    public required string Season { get; init; }

    /// <summary>
    /// Personaje base. Opcional: si no viene, el consumidor lo deduce de la entrada _basic
    /// del mismo personaje. Ej: "Storm Scout".
    /// </summary>
    public string? Character { get; init; }
}
