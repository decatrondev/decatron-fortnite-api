#!/usr/bin/env bash
# Backup del catálogo + imágenes + (opcional) base. Corré desde cron o a mano tras cada ingest.
set -euo pipefail

DATA_DIR="${DATA_DIR:-/var/lib/fortnite-api/data}"
DEST="${DEST:-/var/backups/fortnite-api}"
STAMP="$(date +%Y%m%d-%H%M%S)"

mkdir -p "$DEST"

# 1. data/ (catalog.json, images.json, sprites/) — copia incremental
rsync -a --delete "$DATA_DIR/" "$DEST/data/"

# 2. tarball fechado de data/
tar -C "$DEST" -czf "$DEST/data-$STAMP.tar.gz" data

# 3. base (si se usa Db)
if [ -n "${PGDATABASE:-}" ]; then
  pg_dump --no-owner --format=custom "$PGDATABASE" > "$DEST/db-$STAMP.dump"
fi

# 4. retención: conservar los últimos 14
ls -1t "$DEST"/data-*.tar.gz 2>/dev/null | tail -n +15 | xargs -r rm -f
ls -1t "$DEST"/db-*.dump      2>/dev/null | tail -n +15 | xargs -r rm -f

echo "backup ok -> $DEST (stamp $STAMP)"
