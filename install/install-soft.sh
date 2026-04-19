#!/usr/bin/env bash
# Single-repo Hephaestus: restore solution, build Deploy (DeployDomain) -> repo /release,
# register systemd domainhost, verify it runs.
# Last step of install/install.sh; also: sudo bash install/install-soft.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RELEASE_DIR="$REPO_ROOT/release"
SLN="$REPO_ROOT/panel.sln"
DEPLOY_PROJ="$REPO_ROOT/panel/Deploy/Deploy.csproj"

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$0" "$@"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Install the .NET 9 SDK first (install/install-net.sh)." >&2
  exit 1
fi

if [ ! -f "$SLN" ]; then
  echo "Missing solution: $SLN" >&2
  exit 1
fi
if [ ! -f "$DEPLOY_PROJ" ]; then
  echo "Missing project: $DEPLOY_PROJ" >&2
  exit 1
fi

echo "[1/6] Stop domainhost (service + stray processes)"
if systemctl list-unit-files --type=service 2>/dev/null | grep -q '^domainhost\.service'; then
  systemctl stop domainhost 2>/dev/null || true
fi
pkill -f '[d]otnet.*DomainHost\.dll' 2>/dev/null || true
pkill -f '/release/DomainHost' 2>/dev/null || true
sleep 1

echo "[2/6] Clean and create $RELEASE_DIR"
rm -rf "$RELEASE_DIR"
mkdir -p "$RELEASE_DIR"

echo "[3/6] dotnet restore (whole solution)"
dotnet restore "$SLN" --verbosity minimal

echo "[4/6] dotnet build Deploy -t:DeployDomain -> $RELEASE_DIR"
dotnet build "$DEPLOY_PROJ" -c Release -t:DeployDomain --no-restore -v minimal

echo "[5/6] Install systemd unit domainhost.service (enabled on boot)"
sed "s#@@REPO_ROOT@@#${REPO_ROOT}#g" "$SCRIPT_DIR/domainhost.service" > /etc/systemd/system/domainhost.service
systemctl daemon-reload
systemctl enable domainhost.service

echo "[6/6] Start domainhost and verify"
systemctl restart domainhost.service
sleep 2
if ! systemctl is-active --quiet domainhost.service; then
  echo "ERROR: domainhost.service is not active after start." >&2
  systemctl --no-pager --full status domainhost.service >&2 || true
  journalctl -u domainhost.service -n 80 --no-pager >&2
  exit 1
fi

if ! systemctl is-enabled --quiet domainhost.service 2>/dev/null; then
  echo "WARNING: domainhost.service is not enabled for boot (unexpected)." >&2
fi

systemctl --no-pager --full status domainhost.service || true
echo "domainhost.service is active. Done. Logs: journalctl -u domainhost -f"
