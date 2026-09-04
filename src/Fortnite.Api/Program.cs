// Fortnite.Api — Fase 5. API de solo lectura sobre el catálogo de sprites.
// Origen configurable: File (data/catalog.json) o Db (PostgreSQL).

using System.Text.Json;
using System.Text.Json.Serialization;
using Fortnite.Api;
using Fortnite.Api.SpriteSource;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

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
    p.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET")));

var dataRoot = builder.Configuration["Api:DataRoot"] ?? "data";
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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
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
});

v1.MapGet("/sprites/{id}", async (string id, ISpriteSource src, CancellationToken ct) =>
{
    var all = await src.GetAllAsync(ct);
    var sprite = all.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
    return sprite is null
        ? Results.NotFound(new { error = $"sprite '{id}' no encontrado" })
        : Results.Ok(sprite);
});

// Redirige a la imagen estática con ?v=<hash> para ruptura de caché.
v1.MapGet("/sprites/{id}.png", async (string id, ISpriteSource src, CancellationToken ct) =>
{
    var hashes = await src.GetImageHashesAsync(ct);
    var suffix = hashes.TryGetValue(id, out var h) ? $"?v={h}" : "";
    return Results.Redirect($"/sprites/{id}.png{suffix}", permanent: false);
});

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
});

app.Run();
