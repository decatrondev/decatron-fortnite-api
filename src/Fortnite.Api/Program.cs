// Fortnite.Api — esqueleto (Fase 1).
// Los endpoints reales de /v1/sprites, Swagger y el middleware de API key llegan en la Fase 5.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", phase = 1 }));

app.Run();
