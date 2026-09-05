# Arquitectura

## Principio

Cero terceros en runtime. Los datos se originan en una instalación local de Fortnite y
se sirven íntegramente desde un VPS propio (API + Nginx). Lo único externo es la clave AES
del parche, que es un dato público que se pega a mano al actualizar.

## Flujo de datos

```
Fortnite (.pak/.utoc/.ucas)
        │  clave AES del parche
        ▼
┌───────────────┐   texturas PNG crudas + metadata cruda (JSON)
│ Fortnite.Ingest│ ─────────────────────────────────────────────►  staging/
│  (CUE4Parse)  │
└───────┬───────┘
        │ invoca
        ▼
┌────────────────────┐   PNG 512×512 RGBA normalizados
│ Fortnite.Processing│ ──────────────────────────────────►  data/sprites/<id>.png
│    (ImageSharp)    │   catálogo normalizado ───────────►  data/catalog.json
└───────┬────────────┘
        │ upsert (Fase 4)
        ▼
┌──────────────┐        ┌──────────────────────────┐
│ PostgreSQL   │◄──────►│ Fortnite.Api (ASP.NET)   │  GET /v1/sprites (JSON)
│ sprites +    │        │ solo lectura + API key   │
│ snapshots    │        └──────────────────────────┘
└──────────────┘
                         Nginx  ──►  /sprites/<id>.png   (archivos estáticos, sin pasar por la API)
```

## Responsabilidad por módulo

### Fortnite.Ingest (CLI, manual)
- Entrada: ruta a `Fortnite/FortniteGame/Content/Paks` + clave AES + versión de parche.
- Monta el sistema de archivos virtual con CUE4Parse, localiza los assets de coleccionables.
- Exporta a `staging/`: textura cruda por variante + un JSON con lo que se pueda leer del
  asset (personaje, rareza, temporada, flags de disponibilidad).
- No toca la base de datos ni la API. No corre como servicio.

### Fortnite.Processing (librería)
- Entrada: `staging/`.
- Normaliza cada imagen: recorta el transparente sobrante, centra, rellena a 512×512, RGBA.
- Calcula un hash del contenido para deduplicar y para el parámetro `?v=` de ruptura de caché.
- Asigna `id = SpriteId.From(character, theme)` y arma el `catalog.json` final + `SpriteSnapshot`.

### Fortnite.Api (servicio systemd)
- Solo lectura sobre PostgreSQL.
- `GET /v1/sprites` con filtros (`season`, `theme`, `rarity`, `unreleased`, `character`).
- `GET /v1/sprites/{id}`.
- Swagger en `/swagger`.
- Middleware de API key (header). Sin OAuth por ahora; queda la puerta abierta a reusar el de DecatronAPI.
- Opcional: endpoint/artefacto `sprites-data.js` como copia drop-in para los consumidores que hoy usan ese formato.
- Nunca sirve imágenes: solo devuelve la URL `/sprites/<id>.png`.

### unreleased (dentro de Fortnite.Ingest / SpriteDefinitionReader)
- `Basic` siempre `false` (se obtiene por gameplay normal, no por un pool de variantes).
- El resto se resuelve contra `DT_VariantWeights[_<Season>]`, la tabla de pesos de drop del propio
  juego: peso `0` o fila ausente para esa variante → `unreleased = true`. Es la misma señal que usan
  sitios como fortnite.gg. Se recalcula en cada ingest, no hace falta un job aparte.

### reconcile entre parches (job, Fase 6 — pendiente)
- El diff que ya corre en cada ingest (`SpriteDatabase.DiffAgainstPreviousAsync`) detecta cuándo un
  sprite pasa de `unreleased: true` a `false` (o viceversa) entre un parche y el siguiente.
  Falta automatizarlo con un timer de systemd + una notificación (Discord/Twitch) en vez de que
  quede solo en el log.

### portal/ (estático)
- React + Vite + Tailwind. Landing, documentación, gestión de llaves, dashboard de consumo.
- Se compila a estáticos y lo sirve Nginx en el mismo dominio.

## Infra en el VPS

- **Nginx**: reverse proxy a la API en `localhost` + `location /sprites/` como archivos
  estáticos con `Cache-Control: public, max-age=31536000, immutable`.
- **systemd**: un servicio para la API, un timer para `reconcile`.
- **Let's Encrypt** para TLS.
- **Serilog** a archivo (opcional: Loki → Grafana, como en el resto de proyectos Decatron).
- **Backup**: `rsync` de `data/sprites/` + `pg_dump` de la base tras cada ingest.

## Qué NO se usa y por qué

- Sin Docker/Kubernetes: el deploy es manual con `dotnet publish` + systemd.
- Sin EF Core: son lecturas simples y pocos upserts; Dapper alcanza.
- Sin gRPC ni microservicios: es una API de lectura sobre una tabla.
- Sin Node en el backend: partiría el stack en dos sin necesidad.
- Sin CDN ni almacenamiento de objetos de terceros: lo sirve el propio VPS.
