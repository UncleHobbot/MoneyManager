# Deploying MoneyManager on Synology NAS

This guide covers deploying MoneyManager on a Synology NAS (x86_64) using Docker Compose via Container Manager.

## Prerequisites

- Synology NAS with an **x86_64** CPU (e.g., DS920+, DS1621+, DS723+)
- **Container Manager** installed from Package Center (formerly "Docker" package)
- SSH access enabled (Control Panel → Terminal & SNMP → Enable SSH)

## Quick Start

### 1. Create project directory

SSH into your NAS and create the project folder:

```bash
mkdir -p /volume1/docker/moneymanager
cd /volume1/docker/moneymanager
```

### 2. Download the Compose file

Download `docker-compose.prod.yml` and save it as `docker-compose.yml`:

```bash
curl -L -o docker-compose.yml \
  https://raw.githubusercontent.com/UncleHobbot/MoneyManager/main/docker-compose.prod.yml
```

### 3. Start the container

```bash
sudo docker compose up -d
```

The first run will:
1. Pull the image from GitHub Container Registry
2. Create the named volumes (`mm-data`, `mm-backups`, `mm-csv-archive`)
3. Copy the empty database template into `/app/data/MoneyManager.db`
4. Start the web server on port 8080

### 4. Access the app

Open `http://<NAS-IP>:8080` in your browser.

## Using Synology Container Manager UI

If you prefer the graphical interface:

1. Open **Container Manager** from DSM
2. Go to **Project** → **Create**
3. Set the project name to `moneymanager`
4. Set the path to `/volume1/docker/moneymanager`
5. Paste the contents of `docker-compose.prod.yml` into the compose editor (or upload the file)
6. Click **Next**, review the summary, and click **Done**

Container Manager will pull the image and start the container automatically.

## Volume Mapping

The compose file uses Docker named volumes. On Synology these are stored under `/volume1/@docker/volumes/`:

| Named Volume     | Container Path      | Purpose                          |
|------------------|---------------------|----------------------------------|
| `mm-data`        | `/app/data`         | SQLite database                  |
| `mm-backups`     | `/app/backups`      | Database backup files            |
| `mm-csv-archive` | `/app/csv-archive`  | Archived CSV import files        |

### Using bind mounts instead (optional)

If you prefer visible directories on your NAS, create a custom `docker-compose.yml`:

```yaml
services:
  moneymanager:
    image: ghcr.io/unclehobbot/moneymanager:latest
    container_name: moneymanager
    restart: unless-stopped
    ports:
      - "8080:8080"
    volumes:
      - /volume1/docker/moneymanager/data:/app/data
      - /volume1/docker/moneymanager/backups:/app/backups
      - /volume1/docker/moneymanager/csv-archive:/app/csv-archive
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/MoneyManager.db
```

Pre-create the directories before starting:

```bash
mkdir -p /volume1/docker/moneymanager/{data,backups,csv-archive}
```

## Updating to a New Version

### Via SSH

```bash
cd /volume1/docker/moneymanager
sudo docker compose pull
sudo docker compose up -d
```

### Via Container Manager UI

1. Go to **Project** → select `moneymanager`
2. Click **Action** → **Build** (this pulls the latest image)
3. The container restarts automatically with the new version

### Pin a specific version

To avoid unexpected updates, pin to a version tag in your compose file:

```yaml
image: ghcr.io/unclehobbot/moneymanager:v1.0.0
```

## Changing the Port

Edit your `docker-compose.yml` and change the host port (left side):

```yaml
ports:
  - "9090:8080"   # Access via http://<NAS-IP>:9090
```

Then restart:

```bash
sudo docker compose up -d
```

## Backup Strategy

### Built-in backups

MoneyManager has a built-in backup feature accessible from the Settings page in the web UI. Backups are stored in the `/app/backups` volume.

### Manual SQLite backup

Copy the database file directly from the volume:

```bash
# Named volume
sudo cp /volume1/@docker/volumes/mm-data/_data/MoneyManager.db \
  /volume1/homes/admin/MoneyManager-backup-$(date +%Y%m%d).db

# Bind mount
cp /volume1/docker/moneymanager/data/MoneyManager.db \
  /volume1/homes/admin/MoneyManager-backup-$(date +%Y%m%d).db
```

### Scheduled backup with Synology Task Scheduler

1. Open **Control Panel** → **Task Scheduler**
2. Create → **Scheduled Task** → **User-defined script**
3. Set schedule (e.g., daily at 2:00 AM)
4. In the **Task Settings** tab, enter:

```bash
#!/bin/bash
BACKUP_DIR="/volume1/docker/moneymanager/db-backups"
mkdir -p "$BACKUP_DIR"
# Use the in-app backup API endpoint
curl -s -X POST http://localhost:8080/api/system/backup
# Keep only last 30 backups
ls -t "$BACKUP_DIR"/MoneyManager*.db 2>/dev/null | tail -n +31 | xargs rm -f 2>/dev/null
```

### Restoring from backup

1. Stop the container: `sudo docker compose down`
2. Replace the database file in the data volume with your backup
3. Start the container: `sudo docker compose up -d`

## Troubleshooting

### Check container logs

```bash
sudo docker compose logs -f moneymanager
```

### Check health status

```bash
sudo docker inspect --format='{{.State.Health.Status}}' moneymanager
```

### Container won't start

1. Ensure port 8080 is not in use: `sudo netstat -tlnp | grep 8080`
2. Check volume permissions: the container runs as a non-root user (UID 1654)
3. For bind mounts, ensure directories are writable:
   ```bash
   sudo chown -R 1654:1654 /volume1/docker/moneymanager/data
   ```

### Database locked errors

SQLite does not support concurrent writes. Ensure only one container instance is running. Do not mount the same data volume into multiple containers.
