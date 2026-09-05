// Fortnite.Api — Fase 5. API de solo lectura sobre el catálogo de sprites.
// Origen configurable: File (data/catalog.json) o Db (PostgreSQL).

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Fortnite.Api;
using Fortnite.Api.SpriteSource;
using Fortnite.Persistence;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Config local fuera del repo (connection string, API keys). Opcional.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new()
{
    Title = "Decatron Fortnite API",
    Version = "v1",
    Description = "Catálogo propio de sprites de coleccionable de Fortnite. Solo lectura.",
}));

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET", "POST")));

var dataRoot = builder.Configuration["Api:DataRoot"] ?? "data";
if (!Path.IsPathRooted(dataRoot))
{
    // En prod DataRoot es absoluto. En dev es relativo ("data") y, según cómo se lance
    // (dotnet run vs dotnet Api.dll), el cwd/content root varía. Subimos por el árbol
    // desde el content root y desde el cwd buscando <carpeta>/data/catalog.json.
    dataRoot = ResolveDataRoot(dataRoot, builder.Environment.ContentRootPath)
               ?? ResolveDataRoot(dataRoot, Directory.GetCurrentDirectory())
               ?? Path.GetFullPath(dataRoot);

    static string? ResolveDataRoot(string rel, string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, rel);
            if (File.Exists(Path.Combine(candidate, "catalog.json")))
            {
                return candidate;
            }
        }

        return null;
    }
}

var sourceKind = builder.Configuration["Api:Source"] ?? "File";
var connString = builder.Configuration["Database:ConnectionString"] ?? "";

if (sourceKind.Equals("Db", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(connString))
{
    builder.Services.AddSingleton<ISpriteSource>(new DbSpriteSource(connString));
}
else
{
    builder.Services.AddSingleton<ISpriteSource>(sp =>
        new FileSpriteSource(dataRoot, sp.GetRequiredService<ILogger<FileSpriteSource>>()));
}

// Cuentas / API keys. Requiere Database:ConnectionString; si falta, /v1/keys responde 503
// en vez de romper el arranque (el resto de la API sigue funcionando).
builder.Services.AddSingleton(new ApiKeyStore(connString));
builder.Services.AddSingleton(new SpriteDatabase(connString));

var adminPassword = builder.Configuration["Admin:Password"] ?? "";

// Un solo tier por ahora ("free"): límite parejo para todos. Cuando haya planes pagos,
// esta política se parte por tier (leído de ApiKeyStore) en vez de ser fija.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("free", ctx =>
    {
        var key = ctx.Request.Headers["X-Api-Key"].ToString();
        var partition = string.IsNullOrEmpty(key) ? $"ip:{ctx.Connection.RemoteIpAddress}" : $"key:{key}";
        return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });

    options.AddPolicy("signup", ctx =>
        RateLimitPartition.GetFixedWindowLimiter($"ip:{ctx.Connection.RemoteIpAddress}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
        }));
});

var app = builder.Build();

app.Logger.LogInformation("Origen: {Source} · DataRoot: {DataRoot}", sourceKind, dataRoot);

// Aplica el esquema (tablas/columnas nuevas) al arrancar si hay base configurada. Así un
// deploy por "git pull" + reiniciar el servicio alcanza: no hace falta correr el ingest
// (que necesita Fortnite instalado) solo para migrar la base del VPS.
if (!string.IsNullOrWhiteSpace(connString))
{
    try
    {
        await new SpriteDatabase(connString).EnsureSchemaAsync();
        app.Logger.LogInformation("Esquema de base verificado/actualizado.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "No se pudo verificar/actualizar el esquema de la base.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseRateLimiter();
app.UseMiddleware<ApiKeyMiddleware>();

// PNGs: en dev los sirve esta app; en prod los sirve Nginx directo desde data/sprites.
var spritesDir = Path.GetFullPath(Path.Combine(dataRoot, "sprites"));
if (Directory.Exists(spritesDir))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(spritesDir),
        RequestPath = "/sprites",
        OnPrepareResponse = ctx =>
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable",
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", phase = 5 }));

var v1 = app.MapGroup("/v1");

v1.MapGet("/sprites", async (
    ISpriteSource src,
    string? season, string? theme, string? rarity, bool? unreleased, string? character,
    CancellationToken ct) =>
{
    IEnumerable<SpriteDto> q = await src.GetAllAsync(ct);

    if (season is not null) q = q.Where(s => string.Equals(s.Season, season, StringComparison.OrdinalIgnoreCase));
    if (theme is not null) q = q.Where(s => string.Equals(s.Theme, theme, StringComparison.OrdinalIgnoreCase));
    if (rarity is not null) q = q.Where(s => string.Equals(s.Rarity, rarity, StringComparison.OrdinalIgnoreCase));
    if (unreleased is not null) q = q.Where(s => s.Unreleased == unreleased);
    if (character is not null) q = q.Where(s => string.Equals(s.Character, character, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(q.ToList());
}).RequireRateLimiting("free");

v1.MapGet("/sprites/{id}", async (string id, ISpriteSource src, CancellationToken ct) =>
{
    var all = await src.GetAllAsync(ct);
    var sprite = all.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
    return sprite is null
        ? Results.NotFound(new { error = $"sprite '{id}' no encontrado" })
        : Results.Ok(sprite);
}).RequireRateLimiting("free");

// Redirige a la imagen estática con ?v=<hash> para ruptura de caché.
v1.MapGet("/sprites/{id}.png", async (string id, ISpriteSource src, CancellationToken ct) =>
{
    var hashes = await src.GetImageHashesAsync(ct);
    var suffix = hashes.TryGetValue(id, out var h) ? $"?v={h}" : "";
    return Results.Redirect($"/sprites/{id}.png{suffix}", permanent: false);
}).RequireRateLimiting("free");

// Compat drop-in: mismo catálogo como archivo JS con un global.
v1.MapGet("/sprites-data.js", async (ISpriteSource src, CancellationToken ct) =>
{
    var all = await src.GetAllAsync(ct);
    var json = JsonSerializer.Serialize(all, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    });
    return Results.Text($"window.spritesData = {json};\n", "application/javascript");
}).RequireRateLimiting("free");

// --- Cuentas / API keys ---------------------------------------------------
// Alta libre por email, sin login: te da una key de una vez, no se vuelve a mostrar.
// Sirve para identificar consumidores del free tier hoy; el mismo campo "tier" es
// donde se engancha un plan pago el día que exista, sin tocar el resto de la API.

v1.MapPost("/keys", async (SignupRequest req, ApiKeyStore store, IConfiguration cfg) =>
{
    if (string.IsNullOrWhiteSpace(cfg["Database:ConnectionString"]))
    {
        return Results.Problem("Cuentas no disponibles: falta Database:ConnectionString.", statusCode: 503);
    }

    if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
    {
        return Results.BadRequest(new { error = "email inválido" });
    }

    var issued = await store.IssueKeyAsync(req.Email, req.Name);
    return Results.Ok(new
    {
        apiKey = issued.PlainTextKey,
        tier = issued.Tier,
        note = "Guardá esta clave: no se vuelve a mostrar. Mandala en el header X-Api-Key.",
    });
}).RequireRateLimiting("signup");

v1.MapGet("/keys/me", async (HttpContext ctx, ApiKeyStore store) =>
{
    if (!ctx.Request.Headers.TryGetValue("X-Api-Key", out var provided))
    {
        return Results.Unauthorized();
    }

    var info = await store.ValidateAsync(provided.ToString());
    return info is null ? Results.Unauthorized() : Results.Ok(info);
}).RequireRateLimiting("free");

// --- Admin: corrección manual de unreleased ------------------------------
// Protegido con una clave propia (Admin:Password), independiente de RequireApiKey:
// el panel de admin tiene que estar cerrado siempre, exista o no el resto de la
// autenticación pública. Vacía = panel deshabilitado (403 a todo /v1/admin).

bool IsAdminAuthorized(HttpContext ctx) =>
    !string.IsNullOrWhiteSpace(adminPassword) &&
    ctx.Request.Headers.TryGetValue("X-Admin-Key", out var provided) &&
    provided.ToString() == adminPassword;

var admin = v1.MapGroup("/admin");

admin.MapGet("/sprites", async (HttpContext ctx, SpriteDatabase db) =>
{
    if (!IsAdminAuthorized(ctx))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(connString))
    {
        return Results.Problem("Admin no disponible: falta Database:ConnectionString.", statusCode: 503);
    }

    return Results.Ok(await db.GetAllForAdminAsync());
});

admin.MapPut("/sprites/{id}", async (string id, AdminOverrideRequest req, HttpContext ctx, SpriteDatabase db) =>
{
    if (!IsAdminAuthorized(ctx))
    {
        return Results.Unauthorized();
    }

    await db.SetOverrideAsync(id, req.Unreleased, req.Note);
    return Results.Ok(new { id, req.Unreleased, req.Note });
});

admin.MapDelete("/sprites/{id}/override", async (string id, HttpContext ctx, SpriteDatabase db) =>
{
    if (!IsAdminAuthorized(ctx))
    {
        return Results.Unauthorized();
    }

    await db.ClearOverrideAsync(id);
    return Results.Ok(new { id, cleared = true });
});

app.Run();

sealed record SignupRequest(string Email, string? Name);
sealed record AdminOverrideRequest(bool Unreleased, string? Note);
