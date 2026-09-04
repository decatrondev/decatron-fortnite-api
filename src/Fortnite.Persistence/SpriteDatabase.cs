using System.Reflection;
using Dapper;
using Fortnite.Core.Models;
using Npgsql;

namespace Fortnite.Persistence;

/// <summary>
/// Acceso a la base de sprites con Dapper. Escribe un snapshot por parche y mantiene
/// la tabla <c>sprite</c> con el estado actual. Ofrece un diff contra el parche anterior.
/// </summary>
public sealed class SpriteDatabase(string connectionString)
{
    public sealed record ImageInfo(string? Hash, int? Width, int? Height);

    public sealed record Diff(
        IReadOnlyList<string> Added,
        IReadOnlyList<string> Removed,
        IReadOnlyList<string> NowReleased,
        IReadOnlyList<string> NowUnreleased,
        IReadOnlyList<string> ImageChanged,
        IReadOnlyList<string> MetadataChanged)
    {
        public bool IsEmpty => Added.Count == 0 && Removed.Count == 0 && NowReleased.Count == 0 &&
                               NowUnreleased.Count == 0 && ImageChanged.Count == 0 && MetadataChanged.Count == 0;
    }

    private NpgsqlConnection Open()
    {
        var c = new NpgsqlConnection(connectionString);
        c.Open();
        return c;
    }

    public async Task EnsureSchemaAsync()
    {
        var asm = Assembly.GetExecutingAssembly();
        var resource = asm.GetManifestResourceNames().Single(n => n.EndsWith("schema.sql", StringComparison.OrdinalIgnoreCase));
        await using var stream = asm.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync();

        await using var conn = Open();
        await conn.ExecuteAsync(sql);
    }

    /// <summary>
    /// Reemplaza el snapshot del parche y refresca la tabla <c>sprite</c>. Idempotente.
    /// </summary>
    public async Task WriteSnapshotAsync(
        string patchVersion,
        DateTimeOffset takenAtUtc,
        IReadOnlyList<(Sprite Sprite, ImageInfo Image)> rows)
    {
        await using var conn = Open();
        await using var tx = await conn.BeginTransactionAsync();

        await conn.ExecuteAsync("DELETE FROM snapshot WHERE patch_version = @patchVersion", new { patchVersion }, tx);
        await conn.ExecuteAsync(
            "INSERT INTO snapshot (patch_version, taken_at_utc, sprite_count) VALUES (@patchVersion, @takenAtUtc, @count)",
            new { patchVersion, takenAtUtc, count = rows.Count }, tx);

        const string insSnap = """
            INSERT INTO snapshot_sprite
                (patch_version, id, name, theme, rarity, unreleased, season, character_name, image_hash)
            VALUES
                (@patchVersion, @Id, @Name, @Theme, @Rarity, @Unreleased, @Season, @Character, @Hash)
            """;

        const string upsert = """
            INSERT INTO sprite
                (id, name, theme, rarity, unreleased, season, character_name,
                 image_hash, image_width, image_height, first_seen_patch, last_seen_patch, updated_at_utc)
            VALUES
                (@Id, @Name, @Theme, @Rarity, @Unreleased, @Season, @Character,
                 @Hash, @Width, @Height, @patchVersion, @patchVersion, now())
            ON CONFLICT (id) DO UPDATE SET
                name           = EXCLUDED.name,
                theme          = EXCLUDED.theme,
                rarity         = EXCLUDED.rarity,
                unreleased     = EXCLUDED.unreleased,
                season         = EXCLUDED.season,
                character_name = EXCLUDED.character_name,
                image_hash     = EXCLUDED.image_hash,
                image_width    = EXCLUDED.image_width,
                image_height   = EXCLUDED.image_height,
                last_seen_patch = EXCLUDED.last_seen_patch,
                updated_at_utc  = now()
            """;

        foreach (var (sprite, image) in rows)
        {
            var p = new
            {
                patchVersion,
                sprite.Id,
                sprite.Name,
                sprite.Theme,
                sprite.Rarity,
                sprite.Unreleased,
                sprite.Season,
                sprite.Character,
                image.Hash,
                image.Width,
                image.Height,
            };

            await conn.ExecuteAsync(insSnap, p, tx);
            await conn.ExecuteAsync(upsert, p, tx);
        }

        await tx.CommitAsync();
    }

    /// <summary>Compara el snapshot del parche dado contra el snapshot inmediatamente anterior.</summary>
    public async Task<Diff?> DiffAgainstPreviousAsync(string patchVersion)
    {
        await using var conn = Open();

        var previous = await conn.QueryFirstOrDefaultAsync<string>(
            """
            SELECT patch_version FROM snapshot
            WHERE taken_at_utc < (SELECT taken_at_utc FROM snapshot WHERE patch_version = @patchVersion)
            ORDER BY taken_at_utc DESC
            LIMIT 1
            """, new { patchVersion });

        if (previous is null)
        {
            return null;
        }

        var cur = (await conn.QueryAsync<Row>(
            "SELECT id, unreleased, rarity, name, image_hash AS Hash FROM snapshot_sprite WHERE patch_version = @patchVersion",
            new { patchVersion })).ToDictionary(r => r.Id);

        var prev = (await conn.QueryAsync<Row>(
            "SELECT id, unreleased, rarity, name, image_hash AS Hash FROM snapshot_sprite WHERE patch_version = @previous",
            new { previous })).ToDictionary(r => r.Id);

        var added = cur.Keys.Where(k => !prev.ContainsKey(k)).OrderBy(k => k).ToArray();
        var removed = prev.Keys.Where(k => !cur.ContainsKey(k)).OrderBy(k => k).ToArray();

        var nowReleased = new List<string>();
        var nowUnreleased = new List<string>();
        var imageChanged = new List<string>();
        var metaChanged = new List<string>();

        foreach (var (id, c) in cur)
        {
            if (!prev.TryGetValue(id, out var pr))
            {
                continue;
            }

            if (pr.Unreleased && !c.Unreleased) nowReleased.Add(id);
            if (!pr.Unreleased && c.Unreleased) nowUnreleased.Add(id);
            if (!string.Equals(pr.Hash, c.Hash, StringComparison.Ordinal)) imageChanged.Add(id);
            if (!string.Equals(pr.Rarity, c.Rarity, StringComparison.Ordinal) ||
                !string.Equals(pr.Name, c.Name, StringComparison.Ordinal)) metaChanged.Add(id);
        }

        return new Diff(added, removed,
            nowReleased.OrderBy(x => x).ToArray(),
            nowUnreleased.OrderBy(x => x).ToArray(),
            imageChanged.OrderBy(x => x).ToArray(),
            metaChanged.OrderBy(x => x).ToArray());
    }

    private sealed record Row
    {
        public string Id { get; init; } = "";
        public bool Unreleased { get; init; }
        public string Rarity { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Hash { get; init; }
    }
}
