# Runbook: actualizar tras un parche de Fortnite

Cada vez que Fortnite saca parche (semanal o hotfix), el catálogo puede cambiar.
Todo esto corre en **tu PC** (necesita el juego instalado); al VPS sólo sube `data/`.

## 1. Datos del parche

- Clave AES + versión: abrí `https://fortnite-api.com/v2/aes` → `mainKey` y build.
- `.usmap` nuevo: en FModel, cargá Fortnite → **Tools → Dump Mappings**.
  Queda en `%APPDATA%\FModel\Output\.data\mappings\++Fortnite+Release-<ver>-...usmap`.

## 2. Actualizá la config

En `src/Fortnite.Ingest/appsettings.Local.json`:

- `AesKey`, `PatchVersion`, `MappingsFile` → los del parche nuevo.
- `DynamicKeys` → si `fortnite-api.com/v2/aes` lista claves dinámicas, copialas.
- Copiá el `.usmap` nuevo a `mappings/` del repo.

## 3. Corré el ingest

```
dotnet run --project src/Fortnite.Ingest -- --Ingest:DiscoveryOnly false
```

Produce/actualiza:
- `data/catalog.json`, `data/images.json`, `data/sprites/*.png`
- `staging/<ver>/` con el crudo y el `candidates.txt`
- si `Database:ConnectionString` está seteado: escribe el snapshot y loguea el **diff** contra el parche anterior (nuevos, quitados, cambios de imagen, transiciones de `unreleased`).

## 4. Revisá el diff

- Mirá `staging/<ver>/ingest.log` (líneas `DIFF` y `AVISO`).
- Si aparecen personajes o themes nuevos que el parser no reconoció, ajustá
  `SpriteDefinitionReader.ThemeMap` / `IngestOptions.SeasonNames` y volvé a correr.
- Sprites con icono duplicado (placeholder compartido) suelen ser variantes aún no liberadas:
  candidatas a marcar `unreleased` a mano si hace falta.

## 5. Publicá

```
rsync -a --delete data/ tu-vps:/var/lib/fortnite-api/data/
```

La API con `Api:Source=File` recarga sola al cambiar el mtime de `catalog.json`.
Con `Api:Source=Db`, además reiniciá o esperá al próximo ingest (ya escribió la tabla).

## 6. Verificá

```
curl -s https://fortnite-api.decatron.net/v1/sprites | jq length
curl -s https://fortnite-api.decatron.net/v1/sprites/storm_scout_gold | jq
```
