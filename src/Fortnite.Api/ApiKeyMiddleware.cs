using Fortnite.Persistence;

namespace Fortnite.Api;

/// <summary>
/// Exige el header X-Api-Key cuando Api:RequireApiKey es true. Acepta dos fuentes de clave:
/// una lista de claves "admin" en config (bypass fijo, para uso interno) y las claves emitidas
/// por /v1/keys, validadas contra la base. Si RequireApiKey es false (default), no exige nada:
/// el free tier queda abierto hasta que se decida cerrarlo.
/// </summary>
public sealed class ApiKeyMiddleware(
    RequestDelegate next,
    IConfiguration config,
    ILogger<ApiKeyMiddleware> logger,
    ApiKeyStore store)
{
    private const string HeaderName = "X-Api-Key";

    private readonly HashSet<string> _adminKeys = config.GetSection("Api:ApiKeys").Get<string[]>() is { Length: > 0 } k
        ? new HashSet<string>(k, StringComparer.Ordinal)
        : [];

    private readonly bool _requireKey = config.GetValue("Api:RequireApiKey", false);

    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";

        var isPublic =
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/sprites/", StringComparison.OrdinalIgnoreCase) ||
            // El catálogo de lectura queda público siempre: la landing lo muestra sin cuenta.
            // La alta y la cuenta (POST /v1/keys, GET /v1/keys/me) sí se rigen por RequireApiKey.
            path.StartsWith("/v1/sprites", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/v1/keys/me", StringComparison.OrdinalIgnoreCase) ||
            (HttpMethods.IsPost(ctx.Request.Method) && path.Equals("/v1/keys", StringComparison.OrdinalIgnoreCase)) ||
            // /v1/admin tiene su propia clave (X-Admin-Key, Admin:Password) e independiente de esta.
            path.StartsWith("/v1/admin", StringComparison.OrdinalIgnoreCase);

        if (!_requireKey || isPublic)
        {
            await next(ctx);
            return;
        }

        if (ctx.Request.Headers.TryGetValue(HeaderName, out var provided))
        {
            var key = provided.ToString();
            if (_adminKeys.Contains(key) || await store.ValidateAsync(key) is not null)
            {
                await next(ctx);
                return;
            }
        }

        logger.LogWarning("Rechazado {Path}: API key inválida o ausente.", path);
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = "API key inválida o ausente. Header: " + HeaderName + ". Conseguí una en POST /v1/keys.",
        });
    }
}
