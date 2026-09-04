# Deploy en el VPS (manual)

Todo se levanta a mano. No hay Docker. La API es un servicio systemd detrás de Nginx;
las imágenes las sirve Nginx directo.

## Layout en el servidor

```
/opt/fortnite-api/            binarios publicados (dotnet publish)
/var/lib/fortnite-api/data/   catalog.json, images.json, sprites/*.png   (lo que genera el ingest)
/var/backups/fortnite-api/    backups
```

## 1. Requisitos

```bash
# runtime .NET (no hace falta el SDK en el server)
sudo apt-get install -y aspnetcore-runtime-10.0 nginx
# opcional, si se usa el origen Db:
sudo apt-get install -y postgresql
sudo useradd --system --home /opt/fortnite-api --shell /usr/sbin/nologin fortnite
```

## 2. Publicar (en tu PC) y subir

```bash
dotnet publish src/Fortnite.Api -c Release -o publish/api
rsync -a --delete publish/api/ tu-vps:/opt/fortnite-api/
```

## 3. Datos

El ingest corre en tu PC (necesita Fortnite instalado). Subís sólo la carpeta `data/`:

```bash
rsync -a --delete data/ tu-vps:/var/lib/fortnite-api/data/
sudo chown -R fortnite:fortnite /var/lib/fortnite-api
```

## 4. Servicio

```bash
sudo cp deploy/fortnite-api.service /etc/systemd/system/
# editá Environment= según tu caso (Api__Source, Database__ConnectionString, Api__ApiKeys__0)
sudo systemctl daemon-reload
sudo systemctl enable --now fortnite-api
curl -s http://127.0.0.1:5199/health
```

## 5. Nginx + TLS

```bash
sudo cp deploy/nginx.conf.example /etc/nginx/sites-available/fortnite-api
sudo ln -s /etc/nginx/sites-available/fortnite-api /etc/nginx/sites-enabled/
# ajustá server_name y el alias de /sprites/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d fortnite-api.decatron.net
```

## 6. Backups

```bash
sudo cp deploy/backup.sh /usr/local/bin/fortnite-api-backup
sudo chmod +x /usr/local/bin/fortnite-api-backup
# cron diario:
echo '0 5 * * * fortnite DATA_DIR=/var/lib/fortnite-api/data /usr/local/bin/fortnite-api-backup' | sudo tee /etc/cron.d/fortnite-api-backup
```

## 7. Base de datos (sólo si Api__Source=Db)

```bash
sudo -u postgres createuser fortnite --pwprompt
sudo -u postgres createdb fortnite_api --owner fortnite
# el esquema lo crea el ingest solo (EnsureSchemaAsync) la primera vez que corre con Database:ConnectionString
```

## Actualizar en cada parche

Ver [`runbook-parche.md`](runbook-parche.md).
