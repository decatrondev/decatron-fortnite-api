-- Esquema de la base de sprites. Idempotente: se puede correr en cada arranque del ingest.

CREATE TABLE IF NOT EXISTS sprite (
    id                   text PRIMARY KEY,
    name                 text        NOT NULL,
    theme                text        NOT NULL,
    rarity               text        NOT NULL,
    unreleased           boolean     NOT NULL, -- valor servido: override manual si existe, si no el calculado
    season               text        NOT NULL,
    character_name       text,
    image_hash           text,
    image_width          integer,
    image_height         integer,
    first_seen_patch     text        NOT NULL,
    last_seen_patch      text        NOT NULL,
    updated_at_utc       timestamptz NOT NULL DEFAULT now()
);

-- Migración segura para bases creadas antes de esta columna.
ALTER TABLE sprite ADD COLUMN IF NOT EXISTS computed_unreleased boolean;

CREATE INDEX IF NOT EXISTS ix_sprite_season     ON sprite (season);
CREATE INDEX IF NOT EXISTS ix_sprite_theme      ON sprite (theme);
CREATE INDEX IF NOT EXISTS ix_sprite_unreleased ON sprite (unreleased);

-- Correcciones manuales de unreleased, editables desde el panel /admin del portal.
-- Se reaplican solas en cada ingest futuro (ver SpriteDatabase.WriteSnapshotAsync).
CREATE TABLE IF NOT EXISTS sprite_override (
    id             text        PRIMARY KEY REFERENCES sprite (id) ON DELETE CASCADE,
    unreleased     boolean     NOT NULL,
    note           text,
    updated_at_utc timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS snapshot (
    patch_version text        PRIMARY KEY,
    taken_at_utc  timestamptz NOT NULL,
    sprite_count  integer     NOT NULL
);

-- Foto completa del catálogo tras cada parche, para poder hacer diffs.
CREATE TABLE IF NOT EXISTS snapshot_sprite (
    patch_version  text    NOT NULL REFERENCES snapshot (patch_version) ON DELETE CASCADE,
    id             text    NOT NULL,
    name           text    NOT NULL,
    theme          text    NOT NULL,
    rarity         text    NOT NULL,
    unreleased     boolean NOT NULL,
    season         text    NOT NULL,
    character_name text,
    image_hash     text,
    PRIMARY KEY (patch_version, id)
);

-- Cuentas y API keys para consumidores externos de la API.
-- Por ahora un solo tier ("free"), sin límites diferenciados. Cuando existan planes
-- pagos, se suma una tabla de consumo y se lee "tier" para aplicar límites por plan.
CREATE TABLE IF NOT EXISTS account (
    id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    email           text        NOT NULL UNIQUE,
    created_at_utc  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS api_key (
    id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id      uuid        NOT NULL REFERENCES account (id) ON DELETE CASCADE,
    key_hash        text        NOT NULL UNIQUE, -- SHA-256 hex; la clave en texto plano no se guarda
    name            text,
    tier            text        NOT NULL DEFAULT 'free',
    created_at_utc  timestamptz NOT NULL DEFAULT now(),
    last_used_utc   timestamptz,
    revoked_at_utc  timestamptz
);

CREATE INDEX IF NOT EXISTS ix_api_key_account ON api_key (account_id);
