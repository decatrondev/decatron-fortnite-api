# decatron-fortnite-api

Fuente propia de datos y sprites de coleccionables de Fortnite, para consumo propio
(bot de Twitch de Decatron y herramientas internas). Sin dependencias de terceros en runtime:
los datos salen de una instalación local de Fortnite y todo se sirve desde un VPS propio.

## Qué expone

- **JSON** con un objeto por cada variante de sprite (`id`, `name`, `theme`, `rarity`,
  `unreleased`, `season`, opcional `character`). Formato pensado para que los scripts de
  sync existentes lo lean sin transformar nada.
- **PNG** por cada `id`: RGBA (fondo transparente), 512×512, servido como `/sprites/<id>.png`
  directamente por Nginx.

Detalle del formato en [`docs/modelo-datos.md`](docs/modelo-datos.md).

## Módulos

| Proyecto | Rol | Corre |
|---|---|---|
| `Fortnite.Core` | Modelos y contratos compartidos. Sin dependencias externas. | librería |
| `Fortnite.Ingest` | CLI. Extrae texturas + metadata cruda de los `.pak` con CUE4Parse. | manual, tras cada parche |
| `Fortnite.Processing` | Normaliza imágenes (ImageSharp) y arma el catálogo final. | invocado por Ingest |
| `Fortnite.Api` | ASP.NET Core Minimal API de solo lectura. | servicio systemd en el VPS |
| `portal/` | Web estática (React + Vite + Tailwind): landing, docs, llaves, consumo. | build estático en Nginx |

Flujo completo en [`docs/arquitectura.md`](docs/arquitectura.md).

## Estado

- [x] Fase 1 — Fundaciones: estructura, solución, modelo en `Fortnite.Core`, docs.
- [x] Fase 2 — Ingest (CUE4Parse): 186 sprites + PNG 128x128 del parche 42.10.
- [x] Fase 3 — Processing (ImageSharp): 128x128 RGBA uniforme, hash de contenido, salida en data/.
- [x] Fase 4 — Persistencia (PostgreSQL + Dapper): esquema, snapshot por parche, diff vs parche anterior (opt-in por connection string).
- [x] Fase 5 — API: /v1/sprites (+filtros), /v1/sprites/{id}, /v1/sprites/{id}.png (302 con ?v=hash), /v1/sprites-data.js, Swagger, middleware de API key. Origen File o Db.
- [ ] Fase 6 — Reconcile (`unreleased` automático).
- [x] Fase 7 — Portal: React + Vite + Tailwind, explorador de sprites en vivo + referencia de endpoints (portal/).
- [x] Fase 8 — Deploy: nginx.conf, unit systemd, backup.sh, guia deploy-vps.md y runbook-parche.md.

## Requisitos de desarrollo

- .NET SDK 10
- PostgreSQL 16+ (desde Fase 4)
- Node 20+ (para `portal/`, desde Fase 7)
- Una instalación de Fortnite + la clave AES del parche (solo para `Fortnite.Ingest`)

## Build

```
dotnet build
```

## Licencia y assets

El código de este repo está bajo licencia [MIT](LICENSE).

Los **assets del juego** (sprites, iconos, cualquier PNG extraído de Fortnite) son © Epic Games
y **no se distribuyen en este repo**: `data/`, `staging/` y `mappings/` están en `.gitignore`.
Este proyecto sólo publica el código que los genera y los sirve; el catálogo vivo lo expone la
API desplegada, no este repositorio.
