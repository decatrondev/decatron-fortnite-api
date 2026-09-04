# Modelo de datos

## Objeto Sprite (formato de salida)

Un objeto por cada variante de sprite. Coincide 1:1 con lo que consumen los scripts de sync.

| Campo | Tipo | Req. | Ejemplo | Notas |
|---|---|---|---|---|
| `id` | string | sí | `"stormking_gold"` | Clave única. `personaje_tema` en minúsculas y guion bajo. |
| `name` | string | sí | `"Gold Storm Scout"` | Nombre para mostrar. |
| `theme` | string | sí | `"Gold"` | Variante. Valores en la tabla de abajo. |
| `rarity` | string | sí | `"Special"` | `Rare`, `Special`, `Epic`, `Legendary`, `Mythic`. |
| `unreleased` | boolean | sí | `false` | El campo más importante: si ya se consigue en el juego o no. |
| `season` | string | sí | `"Override"` | Nombre de la temporada. |
| `character` | string | no | `"Storm Scout"` | Si falta, el consumidor lo deduce de la entrada `_basic` del personaje. |

### Themes válidos

`Basic`, `Gold`, `Candy`, `Galaxy`, `Gem`, `Holofoil`, `Rift/Cube`, `Cheat`, `Quack`

> `Rift/Cube` se mantiene con la barra en el valor de salida (es lo que espera el consumidor).
> Para el `id`, `SpriteId.Slug` lo convierte a `rift_cube`.

### Rarezas (orden de menor a mayor)

`Rare` (0) · `Special` (1) · `Epic` (2) · `Legendary` (3) · `Mythic` (4)

## Regla del id

`id = slug(character) + "_" + slug(theme)`

`slug()`: minúsculas, sin acentos, cualquier grupo de caracteres no alfanuméricos se colapsa
en un solo `_`, sin `_` en los extremos.

Ejemplos:
- `("Storm Scout", "Gold")` → `storm_scout_gold`
- `("Cube Queen", "Rift/Cube")` → `cube_queen_rift_cube`
- `("Peely", "Basic")` → `peely_basic`

Validación: `[a-z0-9_]`, sin `__`, sin `_` al inicio o al final. Ver `SpriteId.IsValid`.

## Imagen

| Propiedad | Valor |
|---|---|
| Formato | PNG con canal alfa (RGBA). Nunca JPG. |
| Tamaño | 512 × 512 px |
| Nombre | `<id>.png` |
| Ruta pública | `/sprites/<id>.png` |
| Caché | `Cache-Control: public, max-age=31536000, immutable` (la sirve Nginx) |

Si una imagen se re-genera pero el `id` no cambia, el JSON puede exponer la URL con
`?v=<hash>` para romper la caché de ese sprite puntual.

## Snapshot (histórico)

Por cada ingest se guarda un `SpriteSnapshot`:

| Campo | Tipo | Ejemplo |
|---|---|---|
| `patchVersion` | string | `"34.20"` |
| `takenAtUtc` | timestamp (UTC) | `2026-09-04T12:00:00Z` |
| `sprites` | Sprite[] | catálogo completo de ese ingest |

Sirve para comparar parches y detectar la transición `unreleased: true → false`, que es
el evento que le interesa al bot de Twitch.

## Esquema PostgreSQL (previsto, Fase 4)

- `sprite` — estado actual. PK `id`. Columnas para cada campo del objeto + `image_hash`,
  `first_seen_patch`, `last_updated_utc`.
- `snapshot` — cabecera por ingest. PK `patch_version`. `taken_at_utc`.
- `snapshot_sprite` — filas de cada snapshot (FK a `snapshot`), para poder hacer diffs.

DDL concreto en la Fase 4.
