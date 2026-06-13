# Hephaestus install

| Path | Purpose |
|------|---------|
| `shared/` | Cross-platform data: `setup-postgres.sql`, `domainhost.service`, `install-remote.txt`, `install-remote-creds.txt`, `install-data-creds.txt.example`, `wait.sh` |
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

Clones [hephaestus_data](https://github.com/kgonsovskii/hephaestus_data) as a **sibling** of the hephaestus repo:

```
parent/
  hephaestus/       ← this clone
  hephaestus_data/  ← removed and re-cloned each run
```

Provide a GitHub PAT (read access to `hephaestus_data`) via either:

- Environment variable `HEPHAESTUS_DATA_GITHUB_TOKEN`, or
- File `install/shared/install-data-creds.txt` (one line; copy from `install-data-creds.txt.example`, gitignored)

## Layout

- `linux/common.sh` — `REPO_ROOT`, `hephaestus_data_directory`, sources `shared/wait.sh`
- `win/install-common.ps1` — paths + Chocolatey helpers; uses `shared/setup-postgres.sql`
