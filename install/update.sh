#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /bin/bash "$0" "$@"
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
LOG="${HEPHAESTUS_UPDATE_LOG:-/var/log/hephaestus-update.log}"

mkdir -p "$(dirname "$LOG")" 2>/dev/null || true
exec >"$LOG" 2>&1

if [ -z "${HOME:-}" ]; then
  HOME="$(getent passwd "$(id -u)" 2>/dev/null | cut -d: -f6 || true)"
fi
if [ -z "${HOME:-}" ]; then
  case "$(id -u)" in
    0) HOME=/root ;;
    *) HOME=/tmp ;;
  esac
fi
export HOME
export USER="${USER:-$(id -un)}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$HOME/.dotnet}"
mkdir -p "$DOTNET_CLI_HOME" "$HOME/.nuget/packages" 2>/dev/null || true

LEGACY_UNIT=hephaestus-update-once.service
systemctl disable "$LEGACY_UNIT" 2>/dev/null || true
rm -f "/etc/systemd/system/$LEGACY_UNIT" 2>/dev/null || true
systemctl daemon-reload 2>/dev/null || true

echo "$(date -Is) [update] start REPO_ROOT=$REPO_ROOT HOME=$HOME"

if systemctl list-unit-files --type=service 2>/dev/null | grep -q '^domainhost\.service'; then
  systemctl disable domainhost 2>/dev/null || true
  systemctl stop domainhost 2>/dev/null || true
fi
pkill -f '[d]otnet.*DomainHost\.dll' 2>/dev/null || true
pkill -f '/release/DomainHost' 2>/dev/null || true
sleep 1

if [ -d "$REPO_ROOT/.git" ]; then
  echo "$(date -Is) [update] git fetch + reset --hard"
  git -C "$REPO_ROOT" fetch origin
  branch="$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD)"
  if [ "$branch" = "HEAD" ]; then
    git -C "$REPO_ROOT" reset --hard origin/HEAD
  else
    git -C "$REPO_ROOT" reset --hard "origin/$branch"
  fi
  if [ "${GIT_CLEAN_UNTRACKED:-0}" = "1" ]; then
    git -C "$REPO_ROOT" clean -fd
  fi
else
  echo "$(date -Is) [update] no .git; skip git"
fi

bash "$SCRIPT_DIR/install.sh"
echo "$(date -Is) [update] install finished"
echo "$(date -Is) [update] done"
