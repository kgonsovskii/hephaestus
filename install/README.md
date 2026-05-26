# Hephaestus install

| Path | Purpose |
|------|---------|
| `shared/` | Cross-platform data: `setup-postgres.sql`, `domainhost.service`, `install-remote.txt`, `install-remote-creds.txt`, `wait.sh` |
| `linux/` | Linux install steps (bash, apt) |
| `win/` | Windows install steps (PowerShell, Chocolatey) |
| `Install/`, `InstallRemote/` | .NET tools |

## Entry points

- **Linux:** `sudo bash install/install.sh` (runs `install/linux/*`; DNS before DomainHost)
- **Windows:** `install\install.bat` as Administrator (runs `install\win\*`; DNS before DomainHost)
- **Remote SSH:** `bash install/install-remote.sh`
- **Linux update:** `sudo bash install/update.sh` (runs `install/linux/update.sh`)

## Layout

- `linux/common.sh` — `REPO_ROOT`, `SHARED_DIR`, sources `shared/wait.sh`
- `win/install-common.ps1` — paths + Chocolatey helpers; uses `shared/setup-postgres.sql`
