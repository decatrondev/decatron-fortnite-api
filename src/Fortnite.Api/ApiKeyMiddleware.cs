namespace Fortnite.Api;

/// <summary>
/// Exige el header X-Api-Key contra una lista configurada. Si la lista está vacía, deja pasar todo
/// (modo desarrollo). No aplica a /health ni a /swagger.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration config, ILogger<ApiKeyMiddleware> logger)
{
    private const string HeaderName = "X-Api-Key";

    private readonly HashSet<string> _keys = config.GetSection("Api:ApiKeys").Get<string[]>() is { Length: > 0 } k
        ? new HashSet<string>(k, StringComparer.Ordinal)
        : [];

    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";
        var isPublic = path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("/sprites/", StringComparison.OrdinalIgnoreCase);

        if (_keys.Count == 0 || isPublic)
        {
            await next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var provided) ||
            !_keys.Contains(provided.ToString()))
        {
            logger.LogWarning("Rechazado {Path}: API key inválida o ausente.", path);
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "API key inválida o ausente. Header: " + HeaderName });
            return;
        }

        await next(ctx);
    }
}
