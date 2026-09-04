using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;

namespace Fortnite.Persistence;

/// <summary>
/// Cuentas y API keys. Un email = una cuenta; una cuenta puede tener varias keys.
/// Nunca se guarda la clave en texto plano, sólo su hash. Por ahora todo cae en tier "free".
/// </summary>
public sealed class ApiKeyStore(string connectionString)
{
    public sealed record IssuedKey(string PlainTextKey, string Tier);

    public sealed record KeyInfo(string Email, string? Name, string Tier, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedUtc);

    private NpgsqlConnection Open()
    {
        var c = new NpgsqlConnection(connectionString);
        c.Open();
        return c;
    }

    /// <summary>Crea la cuenta si no existe (por email) y emite una API key nueva. El texto plano no se vuelve a poder leer.</summary>
    public async Task<IssuedKey> IssueKeyAsync(string email, string? name)
    {
        email = email.Trim().ToLowerInvariant();

        await using var conn = Open();
        await using var tx = await conn.BeginTransactionAsync();

        var accountId = await conn.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT id FROM account WHERE email = @email", new { email }, tx);

        accountId ??= await conn.QuerySingleAsync<Guid>(
            "INSERT INTO account (email) VALUES (@email) RETURNING id", new { email }, tx);

        var plainTextKey = "fnapi_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var hash = Hash(plainTextKey);

        await conn.ExecuteAsync(
            "INSERT INTO api_key (account_id, key_hash, name, tier) VALUES (@accountId, @hash, @name, 'free')",
            new { accountId, hash, name }, tx);

        await tx.CommitAsync();
        return new IssuedKey(plainTextKey, "free");
    }

    /// <summary>Valida una clave y, si es válida y no está revocada, actualiza last_used_utc.</summary>
    public async Task<KeyInfo?> ValidateAsync(string plainTextKey)
    {
        var hash = Hash(plainTextKey);

        await using var conn = Open();
        var row = await conn.QueryFirstOrDefaultAsync<KeyRow>(
            """
            SELECT a.email AS Email, k.name AS Name, k.tier AS Tier,
                   k.created_at_utc AS CreatedAtUtc, k.last_used_utc AS LastUsedUtc
            FROM api_key k
            JOIN account a ON a.id = k.account_id
            WHERE k.key_hash = @hash AND k.revoked_at_utc IS NULL
            """, new { hash });

        if (row is null)
        {
            return null;
        }

        await conn.ExecuteAsync("UPDATE api_key SET last_used_utc = now() WHERE key_hash = @hash", new { hash });

        return new KeyInfo(row.Email, row.Name, row.Tier, row.CreatedAtUtc, row.LastUsedUtc);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record KeyRow
    {
        public string Email { get; init; } = "";
        public string? Name { get; init; }
        public string Tier { get; init; } = "";
        public DateTimeOffset CreatedAtUtc { get; init; }
        public DateTimeOffset? LastUsedUtc { get; init; }
    }
}
