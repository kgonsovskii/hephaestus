# Hephaestus install

| Path | Purpose |
|------|---------|
| `shared/` | Cross-platform data: `setup-postgres.sql`, `domainhost.service`, `install-remote.txt`, `install-remote-creds.txt`, `wait.sh` |
| `linux/` | Linux install steps (bash, apt) |
| `win/` | Windows install steps (PowerShell, Chocolatey) |
| `Install/`, `InstallRemote/` | .NET tools |

## Entry points

- **Linux full:** `sudo bash install/install.sh` (order: uninstall, git, net, postgres, **dns**, **soft**)
- **Linux steps:** `sudo bash install/install-dns.sh` then `sudo bash install/install-soft.sh`
- **Windows full:** `install\install.bat` as Administrator (same order: dns before soft)
- **Remote SSH:** `bash install/install-remote.sh`
- **Linux update:** `sudo bash install/update.sh`

## Layout

- `linux/common.sh` — `REPO_ROOT`, `SHARED_DIR`, sources `shared/wait.sh`
- `win/install-common.ps1` — paths + Chocolatey helpers; uses `shared/setup-postgres.sql`
