namespace Fortnite.Core.Models;

/// <summary>
/// Rarezas conocidas de sprite, de menor a mayor. Valores exactos esperados por el consumidor.
/// </summary>
public static class SpriteRarities
{
    public const string Rare = "Rare";
    public const string Special = "Special";
    public const string Epic = "Epic";
    public const string Legendary = "Legendary";
    public const string Mythic = "Mythic";

    public static readonly IReadOnlyList<string> All =
    [
        Rare, Special, Epic, Legendary, Mythic
    ];

    public static bool IsKnown(string rarity) => All.Contains(rarity);

    /// <summary>Orden relativo (0 = Rare). -1 si no se reconoce.</summary>
    public static int Rank(string rarity)
    {
        for (var i = 0; i < All.Count; i++)
        {
            if (All[i] == rarity)
            {
                return i;
            }
        }

        return -1;
    }
}
