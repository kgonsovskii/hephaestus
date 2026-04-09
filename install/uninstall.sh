#!/usr/bin/env bash
# Remove DomainHost systemd service, stop processes, optionally delete the Hephaestus clone directory.
#
#   sudo bash install/uninstall.sh              # full: unregister service + rm -rf repository root
#   sudo bash install/uninstall.sh --full       # same as default
#   sudo bash install/uninstall.sh --for-install # chain only: service + release/; keeps repo (install.sh)
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

MODE=full
case "${1:-}" in
  --for-install) MODE=for_install ;;
  --full | "") MODE=full ;;
  *)
    echo "Usage: $0 [--full | --for-install]" >&2
    exit 1
    ;;
esac

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

echo "[uninstall] Remove published bits under $RELEASE_DIR"
rm -rf "$RELEASE_DIR"

if [ "$MODE" = for_install ]; then
  echo "[uninstall] --for-install: leaving repository at $REPO_ROOT"
  exit 0
fi

echo "[uninstall] --full: deleting repository folder $REPO_ROOT"
rm -rf "$REPO_ROOT"
echo "[uninstall] Done (full)."
