# Hallazgos del descubrimiento — parche 42.10

Corrida: `dotnet run --project src/Fortnite.Ingest` con la clave AES pública de fortnite-api.com.
Build real: `+Fortnite+Release-42.10-CL-57566230`.

- Archivos montados: **1 417 867**
- Candidatos volcados: `staging/42.10/candidates.txt` (27 588, ruidoso)

## Dónde están los sprites de coleccionable

Plugins GameFeature dedicados, uno por temporada:

| Plugin | Carpeta de iconos | Patrón de archivo |
|---|---|---|
| `SpriteLibrary_CH7S3` | `.../Content/UI/` | `T_Icon_BR_Creature_Sprite_<Personaje>_<Theme>_ui.uasset` |
| `SpriteLibrary_Ch7S4` | `.../Content/UI/` | `T_Icon_BR_Creature_Sprite_<Personaje>[_<Theme>].uasset` |

- Los `_L.uasset` son la versión de baja resolución. Se prefiere la que **no** termina en `_L`.
- Texturas full-res de `Creature_Sprite`: ~154. Con `_L`: ~243 más.
- Ojo: el patrón de nombre difiere entre S3 (theme en el medio + sufijo `_ui`) y S4 (theme como sufijo, sin `_ui`).

## Personajes detectados

- **S3:** Boss, BurntPeanut, Drifter, Fishy, King, Llama, Peely, Punk, Seven, Sleepy, Soccer, ZeroPoint
- **S4:** BushRanger, Crown, Dwarf, EightBitBlaster, ImprovedSlide, JazzJackrabbit, Jonesy, Killswitch, Klombo, Overshield, StormScout, WinnerB, WinnerC

`StormScout` (S4) = el ejemplo `stormking_gold` / "Gold Storm Scout" / season "Override" de la spec.
→ **"Override" es el nombre comercial de Ch7 S4.** Falta confirmar el de S3.

## Themes detectados (coinciden con la spec)

S3: base (sin theme), `Gold`, `Galaxy`, `Candy`, `Quack`, `Holofoil`, `Gem`, `Cube`.
La spec pide: Basic, Gold, Candy, Galaxy, Gem, Holofoil, Rift/Cube, Cheat, Quack.
Mapeo: base→`Basic`, `Cube`→`Rift/Cube`, resto 1:1. `Cheat` aparece en S4 como `Cheatmaster`.

S4 (temporada nueva, aún incompleta): `Gold`, `Cheatmaster`, `Hacker` + base.
Que falten themes en S4 es justamente el caso `unreleased: true`.

## Metadata (rarity, unreleased, nombre visible, temporada)

No sale del nombre de archivo. Fuentes candidatas (DataTables del plugin S3):

- `SpriteLibrary_CH7S3/Content/DataTables/DT_SpriteAssetRegistry.uasset` ← registro maestro
- `SpriteLibrary_CH7S3/Content/DataTables/DT_SpriteGenericAssets.uasset`
- `DT_VariantWeights`, `DT_VariantLootWeightTables` ← pesos de drop por variante

Leer DataTables de forma fiable pide un `.usmap` (mappings) del parche.

## Próximo paso (Fase 2b)

1. Enumerar `T_Icon_BR_Creature_Sprite_*` (sin `_L`) en las dos carpetas UI.
2. Parsear `<Personaje>` y `<Theme>` según el patrón de cada plugin.
3. Cargar cada `UTexture2D`, decodificar y exportar PNG a `staging/<patch>/textures/`.
4. Metadata: leer `DT_SpriteAssetRegistry` (con `.usmap`) o, sin mappings, heurística por
   nombre + tabla de overrides manual para rarity/unreleased/season.
