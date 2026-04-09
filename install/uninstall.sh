#!/usr/bin/env bash
# Unregister DomainHost systemd service, stop processes, remove repo /release (published output).
# Does not delete the Hephaestus repository clone.
#
# Run: sudo bash install/uninstall.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RELEASE_DIR="$REPO_ROOT/release"

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$0" "$@"
fi

echo "[uninstall] Stop DomainHost (systemd + processes)"
if systemctl list-unit-files --type=service 2>/dev/null | grep -q '^domainhost\.service'; then
  systemctl stop domainhost 2>/dev/null || true
  systemctl disable domainhost 2>/dev/null || true
fi
pkill -f '[d]otnet.*DomainHost\.dll' 2>/dev/null || true
pkill -f '/release/DomainHost' 2>/dev/null || true
sleep 1

if [ -f /etc/systemd/system/domainhost.service ]; then
  rm -f /etc/systemd/system/domainhost.service
  systemctl daemon-reload
fi

echo "[uninstall] Remove $RELEASE_DIR"
rm -rf "$RELEASE_DIR"

echo "[uninstall] Done."
