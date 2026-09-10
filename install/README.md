# Hephaestus install

| Path | Purpose |
|------|---------|
| `shared/` | Cross-platform data: `setup-postgres.sql`, `domainhost.service`, `install-remote.txt`, `install-remote-creds.txt` (host/login/password/profile per server; all hosts in parallel), `wait.sh` |
| `linux/` | Linux install steps (bash, apt) |
| `win/` | Windows install steps (PowerShell, Chocolatey) |
| `Install/`, `InstallRemote/` | .NET tools |

## Entry points

- **Linux full:** `sudo bash install/install.sh` (order: uninstall, git, net, postgres, **dns**, **data**, **soft**)
- **Linux steps:** `sudo bash install/install-data.sh` then `sudo bash install/install-soft.sh`
- **Windows full:** `install\install.bat` as Administrator (same order: dns → data → soft)
- **Windows data only:** `install\win\install-data.bat` (Administrator)
- **Remote SSH:** `bash install/install-remote.sh` (Windows: `install\install-remote.bat`) — installs every host in `shared/install-remote-creds.txt` in parallel (each server’s profile is validated and `$HOME/profile.txt` is overwritten on the target). Optional extra args still run a single host: `[profile] [host] [login] [password]`.
- **Linux update:** `sudo bash install/update.sh`

### `install-data` (before soft)

Clones [hephaestus_data](https://github.com/kgonsovskii/hephaestus_data) as a **sibling** of the hephaestus repo. DomainHost also syncs it on start and on `Refiner:HephaestusDataInterval` / CP apply (`panel/Git`). PAT is in `panel/Git/HephaestusDataGitConstants.cs` and install-data scripts.

```
parent/
  hephaestus/       ← this clone
  hephaestus_data/  ← removed and re-cloned by install-data; synced by DomainHost thereafter
```

## Layout

- `linux/common.sh` — `REPO_ROOT`, `hephaestus_data_directory`, sources `shared/wait.sh`
- `win/install-common.ps1` — paths + Chocolatey helpers; uses `shared/setup-postgres.sql`
