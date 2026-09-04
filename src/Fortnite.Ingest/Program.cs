// Fortnite.Ingest — esqueleto (Fase 1).
// En la Fase 2 este CLI:
//   1. recibe la ruta a los .pak de Fortnite, la clave AES y la versión del parche,
//   2. monta el sistema de archivos virtual con CUE4Parse,
//   3. localiza los assets de coleccionables y exporta textura + metadata cruda a staging/.

using Fortnite.Core.Models;

Console.WriteLine($"Fortnite.Ingest — Fase 1 (esqueleto). Themes conocidos: {string.Join(", ", SpriteThemes.All)}");
