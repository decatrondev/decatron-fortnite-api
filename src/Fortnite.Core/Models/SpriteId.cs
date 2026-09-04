using System.Globalization;
using System.Text;

namespace Fortnite.Core.Models;

/// <summary>
/// Construcción y validación del id de sprite: personaje_tema en minúsculas y guion bajo.
/// </summary>
public static class SpriteId
{
    /// <summary>
    /// Arma un id a partir de personaje y theme.
    /// Ej: ("Storm Scout", "Gold") -> "storm_scout_gold"; ("Cube Queen", "Rift/Cube") -> "cube_queen_rift_cube".
    /// </summary>
    public static string From(string character, string theme) =>
        $"{Slug(character)}_{Slug(theme)}";

    /// <summary>Normaliza un texto a minúsculas, sin acentos, con guion bajo como separador.</summary>
    public static string Slug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastWasSeparator = false;

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator && sb.Length > 0)
            {
                sb.Append('_');
                lastWasSeparator = true;
            }
        }

        return sb.ToString().Trim('_');
    }

    /// <summary>true si el id respeta el formato esperado: [a-z0-9_], sin guiones bajos dobles ni en los extremos.</summary>
    public static bool IsValid(string id)
    {
        if (string.IsNullOrEmpty(id) || id[0] == '_' || id[^1] == '_')
        {
            return false;
        }

        var previousWasUnderscore = false;
        foreach (var ch in id)
        {
            var ok = ch is '_' || (ch is >= 'a' and <= 'z') || (ch is >= '0' and <= '9');
            if (!ok)
            {
                return false;
            }

            if (ch == '_' && previousWasUnderscore)
            {
                return false;
            }

            previousWasUnderscore = ch == '_';
        }

        return true;
    }
}
