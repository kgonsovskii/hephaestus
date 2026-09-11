# Hephaestus

Панель DomainHost + DNS (Technitium). Профиль — соседний `profile.txt` (каталог рядом с репо).

```
parent/
  profile.txt
  hephaestus/          ← этот репозиторий
  hephaestus_data/     ← данные (клонирует install-data)
```

## Установка на сервер

Полный цикл: uninstall DomainHost → git → .NET 10 → PostgreSQL → Technitium DNS → данные → DomainHost.

**Technitium:** если уже стоит (`dns.service` + `/opt/technitium/dns/DnsServerApp.dll`, Windows: служба `hephaestus-dns`), шаг пропускается. Переустановка только с `--force` / `-Force`.

| Скрипт | Что делает | Аргументы |
|--------|------------|-----------|
| `install/install.sh` | Полная установка Linux | `[профиль] [--force]` |
| `install/install.bat` | То же на Windows (Admin) | `[профиль] [-Force]` |
| `install/linux/install-dns.sh` | Только Technitium | `[--force]` |
| `install/win/install-dns.bat` | Только Technitium (Win) | `[-Force]` |
| `install/linux/install-git.sh` | git + ca-certificates (пропуск, если пакет есть) | — |
| `install/linux/install-net.sh` | .NET 10 SDK/runtime | — |
| `install/linux/install-postgres.sh` | PostgreSQL + SQL | — |
| `install/linux/install-data.sh` | Клон `hephaestus_data` (сначала удаляет) | — |
| `install/linux/install-soft.sh` | Сборка DomainHost + systemd | — |
| `install/uninstall.sh` | Стоп DomainHost, `release/` | DNS **не** трогает |
| `install/update.sh` | git reset + полный `install.sh` (DNS без `--force` = skip) | `[профиль]` |

Шаги Windows: `install/win/install-*.bat` (Admin), те же роли.

## Удалённая установка (SSH)

Хосты: `install/shared/install-remote-creds.txt` — четверки **host / login / password / profile** (все сразу, параллельно). Пароли в README не пишем.

| Скрипт | Что делает | Аргументы |
|--------|------------|-----------|
| `install/install-remote.sh` | SSH → clone → `install.sh` | `[профиль] [--force] [хост] [логин] [пароль]` |
| `install/install-remote.bat` | То же с Windows | то же |
| `install-remote.bat` | Ярлык на `install/install-remote.bat` | `%*` |
| `install/install-remote-profile.bat` | Спросит профиль, затем remote | дальше как remote |
| `install/linux/fix-remote-dns.sh` | Починка резолвера / Technitium на уже стоящем хосте | — |

Без `--force` DNS на сервере не пересобирается.

## Прочее

| Скрипт | Что делает | Аргументы |
|--------|------------|-----------|
| `push.bat` | `git add/commit/push` | сообщение захардкожено |
| `pull.bat` | `git pull` | — |
| `clean.bat` | Удаляет все `bin/` и `obj/` | — |
| `troyan-builder.bat` | Сборка и запуск TroyanBuilder | `%*` в exe |

Консоль Technitium: `http://<хост>:5380/`.
