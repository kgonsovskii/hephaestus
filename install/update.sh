#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /bin/bash "$0" "$@"
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "$(date -Is) [update] start REPO_ROOT=$REPO_ROOT"

if systemctl list-unit-files --type=service 2>/dev/null | grep -q '^domainhost\.service'; then
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
echo "$(date -Is) [update] done"
