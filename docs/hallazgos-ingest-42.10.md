# Hallazgos del ingest — parche 42.10

Build real: `+Fortnite+Release-42.10-CL-57566230`. Mappings: `.usmap` de FModel (variante `_zs`).

## Resultado de la Fase 2c

- **186 sprites** en `staging/42.10/catalog.json` (formato spec, keys en minúscula).
- **186 PNG** en `staging/42.10/textures/` a **128×128** (resolución nativa; el juego no trae más grande).
- `staging/42.10/raw/*.json` — un `RawSprite` por sprite con notas de trazabilidad.
- `staging/42.10/registry/*.json` — DataTables volcadas como referencia.
- `staging/42.10/dump/*.json` — assets sueltos volcados con `--Ingest:DumpAsset`.

## Fuente de verdad: assets `ESD_*`

`Class = ExtractableItemDefinition`, en `SpriteLibrary_*/Content/SpriteDefinitions/<Arquetipo>/ESD_*.uasset`.
Campos usados (algunos vienen aplanados de `DataList[]`):

| Campo del juego | → spec |
|---|---|
| `ItemName.SourceString` (se le quita el sufijo " Sprite") | `name` |
| `Rarity` = `EFortRarity::X` | `rarity` = `X` |
| `VariantRarityTag.TagName` = `Extraction.VariantRarity.<Token>` | `theme` (mapeado) |
| `DataList[].Icon.AssetPathName` | textura a decodificar |
| `DexNumber` | nº de colección (va en `raw`, no en la spec) |
| carpeta del plugin | `season` (vía `IngestOptions.SeasonNames`) |

- ESD base (sin `_Variant_`) → `theme = Basic` y aporta el `character` a sus variantes.
- `ESD_*_Variant_A` → `VariantRarityTag = UseArchetype`: es un placeholder que reusa la base. **Se descarta** (40 casos).

## Mapeo de themes

`Gold`, `Candy` (el juego lo llama *Gummy* en el `name`), `Galaxy`, `Gem`, `Holofoil`, `Cube`→`Rift/Cube`,
`CheatMaster`→`Cheat`, `Quack`. Sin token → `Basic`.
`Hacker` (de `LootHacker`, S4) no está en la spec → se deja con el token crudo, por decisión del proyecto.

## Temporadas

| Plugin | `season` |
|---|---|
| `SpriteLibrary_CH7S3` | `Runners` (Ch7 S3) |
| `SpriteLibrary_Ch7S4` | `Override` (Ch7 S4) |

## Distribución del catálogo 42.10

- themes: Basic 40, Gold 35, Candy 20, Galaxy 20, Cheat 15, Hacker 15, Holofoil 15, Gem 13, Rift/Cube 9, Quack 4
- rarezas: Rare 62, Legendary 47, Epic 44, Mythic 33
- seasons: Runners 124, Override 62

## Pendientes / notas

- **`rarity` "Special"** de la spec no existe en el juego (`EFortRarity` no la tiene). Nuestros valores
  autoritativos son Rare/Epic/Legendary/Mythic. Si las otras fuentes usan "Special", hay que decidir un mapeo.
- **`unreleased`** = `false` en todo el catálogo (todo lo que tiene ESD ya está liberado). El aporte como
  tercera fuente es detectar en el diff entre parches cuando aparezca un icono sin ESD.
- **`character`** en S3 usa el nombre interno del arquetipo (`Air`, `Water`, `Fire`, `Earth`, `Grim`…),
  que es lo que dice `ItemName`. Si las otras fuentes usan otro nombre, va una tabla de alias.
