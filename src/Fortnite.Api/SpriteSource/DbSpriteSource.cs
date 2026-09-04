using Dapper;
using Npgsql;

namespace Fortnite.Api.SpriteSource;

/// <summary>Lee la tabla <c>sprite</c> de PostgreSQL.</summary>
public sealed class DbSpriteSource(string connectionString) : ISpriteSource
{
    public async Task<IReadOnlyList<SpriteDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.QueryAsync<SpriteDto>(
            """
            SELECT id, name, theme, rarity, unreleased, season, character_name AS Character
            FROM sprite
            ORDER BY season, character_name, theme
            """);
        return rows.ToList();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetImageHashesAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.QueryAsync<(string Id, string? Hash)>(
            "SELECT id, image_hash FROM sprite WHERE image_hash IS NOT NULL");
        return rows.ToDictionary(r => r.Id, r => r.Hash!, StringComparer.OrdinalIgnoreCase);
    }
}
