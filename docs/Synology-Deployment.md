# Deploying MoneyManager on Synology NAS

This guide covers publishing a MoneyManager release and deploying it on a Synology NAS (x86_64, **DSM 7.3.2**) using Docker Compose via Container Manager.

It has two parts:

1. **[Publishing a release](#part-1-publishing-a-release)** — tag the repo so GitHub Actions builds the image and pushes it to GHCR.
2. **[Deploying to the NAS](#part-2-deploying-to-the-nas)** — pull that image onto the Synology and run it.

## Prerequisites

- Synology NAS with an **x86_64** CPU (e.g., DS920+, DS1621+, DS723+) running **DSM 7.3.2**
- **Container Manager** installed from Package Center (the package formerly called "Docker")
- SSH access enabled (Control Panel → Terminal & SNMP → Enable SSH)
- A GitHub account with push access to this repo, for Part 1

## Part 1: Publishing a Release

The image isn't built until you push a version tag — there's no standing `latest` build to pull until this has happened at least once.

`.github/workflows/deploy.yml` triggers on tags matching `v*`: it builds the Docker image and pushes `ghcr.io/unclehobbot/moneymanager:<tag>` and `:latest` to the GitHub Container Registry (GHCR).

### 1. Tag and push a release

```bash
git tag v1.0.0
git push origin v1.0.0
```

### 2. Watch the build

```bash
gh run watch --repo UncleHobbot/MoneyManager
```

Or check the **Actions** tab on GitHub. Once it's green, the image is live at `ghcr.io/unclehobbot/moneymanager:v1.0.0` (and `:latest`).

### 3. Decide on package visibility

**New GHCR packages are private by default**, even in a public repo. A private package needs authentication to pull — including from your own NAS. Pick one:

- **Make the package public** (simplest if the image itself has no secrets baked in — it doesn't): on GitHub, go to your profile → **Packages** → `moneymanager` → **Package settings** → **Change visibility** → **Public**.
- **Keep it private** and authenticate the NAS instead (see [Authenticating to a private GHCR package](#authenticating-to-a-private-ghcr-package) below).

## Part 2: Deploying to the NAS

### Quick Start (SSH)

#### 1. Create project directory

SSH into your NAS and create the project folder:

```bash
mkdir -p /volume1/docker/moneymanager
cd /volume1/docker/moneymanager
```

#### 2. Authenticate to GHCR (skip if the package is public)

```bash
sudo docker login ghcr.io -u <your-github-username>
# Password: a GitHub Personal Access Token with `read:packages` scope
```

See [Authenticating to a private GHCR package](#authenticating-to-a-private-ghcr-package) for how to create that token.

#### 3. Download the Compose file

Download `docker-compose.prod.yml` and save it as `docker-compose.yml`:

```bash
curl -L -o docker-compose.yml \
  https://raw.githubusercontent.com/UncleHobbot/MoneyManager/main/docker-compose.prod.yml
```

#### 4. Start the container

```bash
sudo docker compose up -d
```

The first run will:
1. Pull the image from GitHub Container Registry
2. Create the named volumes (`mm-data`, `mm-backups`, `mm-csv-archive`)
3. Copy the empty database template into `/app/data/MoneyManager.db`
4. Start the web server on port 8080

#### 5. Access the app

Open `http://<NAS-IP>:8080` in your browser.

## Using Synology Container Manager UI (DSM 7.3.2)

If you prefer the graphical interface over SSH:

### Authenticate to GHCR first (skip if the package is public)

1. Open **Container Manager** → **Registry** tab
2. Click **Settings** (gear icon) → **Add** under registry sources, or use **Registry** → **⋮** → **Add Registry**
3. Add `ghcr.io` with your GitHub username and a Personal Access Token (`read:packages` scope) as the password — see [Authenticating to a private GHCR package](#authenticating-to-a-private-ghcr-package)

### Create the project

1. Open **Container Manager** from DSM
2. Go to **Project** → **Create**
3. Set the project name to `moneymanager`
4. Set the path to `/volume1/docker/moneymanager` (must include the volume number, e.g. `/volume1/...`)
5. Under **Source**, choose **Create docker-compose.yml** and paste the contents of `docker-compose.prod.yml` (or choose **Upload compose.yml** if you downloaded the file first)
6. Click **Next**, review the resource summary, and click **Done**

Container Manager will pull the image and start the container automatically. Watch progress under **Project** → `moneymanager` → **Build Status**.

### Access the app

Open `http://<NAS-IP>:8080` in your browser.

## Authenticating to a Private GHCR Package

If you didn't make the package public in [Part 1](#3-decide-on-package-visibility), the NAS needs a GitHub Personal Access Token to pull it.

1. On GitHub: **Settings** → **Developer settings** → **Personal access tokens** → **Fine-grained tokens** → **Generate new token**
2. Scope it to **read:packages** only (no repo write access needed)
3. Use the token as the password when running `docker login ghcr.io` (SSH) or adding the `ghcr.io` registry in Container Manager (GUI) — the username is your GitHub username, not your NAS account

The NAS caches this login, so you only need to do it once per Docker daemon (until the token expires or is revoked).

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

## Publishing a Follow-Up Release

Once the first release is live, shipping an update is just Part 1 again with a new tag:

```bash
git tag v1.0.1
git push origin v1.0.1
```

Then pull it on the NAS — see [Updating to a New Version](#updating-to-a-new-version) below.

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

### `pull access denied` / `403 Forbidden` when pulling the image

The GHCR package is private and the NAS isn't authenticated. Either make the package public (Part 1, step 3) or run `sudo docker login ghcr.io` with a PAT as described in [Authenticating to a private GHCR package](#authenticating-to-a-private-ghcr-package).

### `manifest unknown` when pulling a tag

No release has been published yet, or the tag doesn't match an existing one. Confirm the tag exists at `https://github.com/UncleHobbot/MoneyManager/pkgs/container/moneymanager`, or push a new tag (Part 1).

### Database locked errors

SQLite does not support concurrent writes. Ensure only one container instance is running. Do not mount the same data volume into multiple containers.
