#!/usr/bin/env bash
# Publish DomainHost (Release -> repo /release), register systemd domainhost (enabled), start service.
# Last step of install/install.sh; also: sudo bash install/install-soft.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RELEASE_DIR="$REPO_ROOT/release"
PUBLISH_PROJ="$REPO_ROOT/src/PublishDomainHost/PublishDomainHost.csproj"

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$0" "$@"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Install the .NET 9 SDK first (install/install-net.sh)." >&2
  exit 1
fi

echo "[1/5] Stop DomainHost (service + stray processes)"
if systemctl list-unit-files --type=service 2>/dev/null | grep -q '^domainhost\.service'; then
  systemctl stop domainhost 2>/dev/null || true
fi
pkill -f '[d]otnet.*DomainHost\.dll' 2>/dev/null || true
pkill -f '/release/DomainHost' 2>/dev/null || true
sleep 1

echo "[2/5] Clean and create $RELEASE_DIR"
rm -rf "$RELEASE_DIR"
mkdir -p "$RELEASE_DIR"

echo "[3/5] Publish Release to release/"
dotnet build "$PUBLISH_PROJ" -c Release -t:PublishDomainHost -v minimal

echo "[4/5] Install systemd unit (enable on boot)"
sed "s#@@REPO_ROOT@@#${REPO_ROOT}#g" "$SCRIPT_DIR/domainhost.service" > /etc/systemd/system/domainhost.service
systemctl daemon-reload
systemctl enable domainhost.service

echo "[5/5] Start domainhost"
systemctl restart domainhost.service
systemctl --no-pager --full status domainhost.service || true

echo "Done. Check: journalctl -u domainhost -f"
