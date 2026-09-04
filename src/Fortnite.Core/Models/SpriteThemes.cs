namespace Fortnite.Core.Models;

/// <summary>
/// Variantes conocidas de sprite. Los valores son los strings exactos que espera el consumidor.
/// </summary>
public static class SpriteThemes
{
    public const string Basic = "Basic";
    public const string Gold = "Gold";
    public const string Candy = "Candy";
    public const string Galaxy = "Galaxy";
    public const string Gem = "Gem";
    public const string Holofoil = "Holofoil";
    public const string RiftCube = "Rift/Cube";
    public const string Cheat = "Cheat";
    public const string Quack = "Quack";

    public static readonly IReadOnlyList<string> All =
    [
        Basic, Gold, Candy, Galaxy, Gem, Holofoil, RiftCube, Cheat, Quack
    ];

    public static bool IsKnown(string theme) => All.Contains(theme);
}
