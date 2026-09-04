# Convenciones

## Repo

- Un solo `.sln`: `DecatronFortniteApi.sln`. Todo el backend en `src/`.
- El portal web vive en `portal/` con su propio `package.json`, build independiente.
- `staging/`, `data/`, `sprites/` y los `.pak` nunca van al repo (ver `.gitignore`).
- Secretos en `appsettings.Local.json` / `.env` locales, fuera del repo.

## Código .NET

- C# con nullable habilitado y `ImplicitUsings` (por defecto en las plantillas net10).
- Modelos de dominio como `record` inmutables (`init`), en `Fortnite.Core/Models`.
- `Fortnite.Core` no toma dependencias de paquetes: solo BCL.
- Nombres y comentarios en español, consistente con el resto de proyectos Decatron.
- Los valores de `theme` / `rarity` / `season` viajan como string para fidelidad con el
  consumidor; las constantes conocidas están en `SpriteThemes` / `SpriteRarities`.

## Versionado de la API

- Prefijo `/v1`. Un cambio incompatible en el formato de salida = `/v2`, sin romper `/v1`.

## Ingest

- Cada corrida se identifica por `patchVersion` (ej. `34.20`).
- La clave AES se pasa por parámetro o archivo local `aes.key`, nunca hardcodeada.
- El ingest es idempotente: volver a correr el mismo parche reemplaza su snapshot.

## Imágenes

- Salida siempre PNG RGBA 512×512. La validación de esto vive en `Fortnite.Processing`.
- `image_hash` = hash del contenido del PNG final; se usa para deduplicar y para `?v=`.

## Commits

- Mensajes en español, imperativo: "agrega modelo de Sprite", "conecta Ingest con Core".
- Un commit por unidad lógica; la fase no es un commit único.
