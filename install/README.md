# Hephaestus install

| Path | Purpose |
|------|---------|
| `shared/` | Cross-platform data: `setup-postgres.sql`, `domainhost.service`, `install-remote.txt`, `install-remote-creds.txt`, `wait.sh` |
| `linux/` | Linux install steps (bash, apt) |
| `win/` | Windows install steps (PowerShell, Chocolatey) |
| `Install/`, `InstallRemote/` | .NET tools |

## Entry points

- **Linux full:** `sudo bash install/install.sh` (order: uninstall, git, net, postgres, **dns**, **data**, **soft**)
- **Linux steps:** `sudo bash install/install-data.sh` then `sudo bash install/install-soft.sh`
- **Windows full:** `install\install.bat` as Administrator (same order: dns → data → soft)
- **Windows data only:** `install\win\install-data.bat` (Administrator)
- **Remote SSH:** `bash install/install-remote.sh`
- **Linux update:** `sudo bash install/update.sh`

### `install-data` (before soft)

Stub step kept for install order compatibility. **`hephaestus_data` is cloned and synced by DomainHost** (`panel/Git`) on startup and every 24 hours (`Refiner:HephaestusDataInterval`). GitHub URL and PAT live in `panel/Git/HephaestusDataGitConstants.cs`.

```
parent/
  hephaestus/       ← this clone
  hephaestus_data/  ← sibling; cloned by DomainHost if missing
```

## Layout

- `linux/common.sh` — `REPO_ROOT`, `hephaestus_data_directory`, sources `shared/wait.sh`
- `win/install-common.ps1` — paths + Chocolatey helpers; uses `shared/setup-postgres.sql`
