#!/usr/bin/env bash
# Restore full solution, build Deploy (DeployDomain target) -> repo /release, register systemd domainhost, verify it runs.
# Last step of install/install.sh; also: sudo bash install/install-soft.sh
#
# Clones sibling repos (same parent directory as this hephaestus clone) before replacing folders.
# Defaults: https://github.com/kgonsovskii/hephaestus_{data,distrib,client}
# Override: export HEPHAESTUS_DATA_REPO / HEPHAESTUS_DISTRIB_REPO / HEPHAESTUS_CLIENT_REPO
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RELEASE_DIR="$REPO_ROOT/release"
SLN="$REPO_ROOT/src/hephaestus.sln"
DEPLOY_PROJ="$REPO_ROOT/src/Deploy/Deploy.csproj"
PARENT_DIR="$(cd "$REPO_ROOT/.." && pwd)"
HEPHAESTUS_DATA_DIR="$PARENT_DIR/hephaestus_data"
HEPHAESTUS_DISTRIB_DIR="$PARENT_DIR/hephaestus_distrib"
HEPHAESTUS_CLIENT_DIR="$PARENT_DIR/hephaestus_client"
_DEFAULT_HEPHAESTUS_DATA_REPO='https://github.com/kgonsovskii/hephaestus_data.git'
_DEFAULT_HEPHAESTUS_DISTRIB_REPO='https://github.com/kgonsovskii/hephaestus_distrib.git'
_DEFAULT_HEPHAESTUS_CLIENT_REPO='https://github.com/kgonsovskii/hephaestus_client.git'
HEPHAESTUS_DATA_REPO="${HEPHAESTUS_DATA_REPO:-$_DEFAULT_HEPHAESTUS_DATA_REPO}"
HEPHAESTUS_DISTRIB_REPO="${HEPHAESTUS_DISTRIB_REPO:-$_DEFAULT_HEPHAESTUS_DISTRIB_REPO}"
HEPHAESTUS_CLIENT_REPO="${HEPHAESTUS_CLIENT_REPO:-$_DEFAULT_HEPHAESTUS_CLIENT_REPO}"

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

if ! command -v git >/dev/null 2>&1; then
  echo "git not found (required to clone data/distrib/client). Install Git first (install/install-git.sh)." >&2
  exit 1
fi

# Clone into a fresh directory first, then remove the old sibling folder — never delete before a successful clone.
clone_then_replace_dir() {
  local url=$1
  local dest=$2
  local env_hint=$3
  if [ -z "$url" ]; then
    echo "[install-soft] Skip clone $dest (export $env_hint to enable)."
    return 0
  fi
  local staging="${dest}.clone.$$"
  rm -rf "$staging"
  git clone --depth 1 "$url" "$staging"
  rm -rf "$dest"
  mv "$staging" "$dest"
  echo "[install-soft] Replaced $dest from $url"
}

echo "[1/7] Clone sibling repos (hephaestus_data, hephaestus_distrib, hephaestus_client) before replacing folders"
clone_then_replace_dir "$HEPHAESTUS_DATA_REPO" "$HEPHAESTUS_DATA_DIR" "HEPHAESTUS_DATA_REPO"
clone_then_replace_dir "$HEPHAESTUS_DISTRIB_REPO" "$HEPHAESTUS_DISTRIB_DIR" "HEPHAESTUS_DISTRIB_REPO"
clone_then_replace_dir "$HEPHAESTUS_CLIENT_REPO" "$HEPHAESTUS_CLIENT_DIR" "HEPHAESTUS_CLIENT_REPO"

echo "[2/7] Stop domainhost (service + stray processes)"
if systemctl list-unit-files --type=service 2>/dev/null | grep -q '^domainhost\.service'; then
  systemctl stop domainhost 2>/dev/null || true
fi
pkill -f '[d]otnet.*DomainHost\.dll' 2>/dev/null || true
pkill -f '/release/DomainHost' 2>/dev/null || true
sleep 1

echo "[3/7] Clean and create $RELEASE_DIR"
rm -rf "$RELEASE_DIR"
mkdir -p "$RELEASE_DIR"

echo "[4/7] dotnet restore (whole solution)"
dotnet restore "$SLN" --verbosity minimal

echo "[5/7] dotnet build Deploy -t:DeployDomain -> $RELEASE_DIR"
dotnet build "$DEPLOY_PROJ" -c Release -t:DeployDomain --no-restore -v minimal

echo "[6/7] Install systemd unit domainhost.service (enabled on boot)"
sed "s#@@REPO_ROOT@@#${REPO_ROOT}#g" "$SCRIPT_DIR/domainhost.service" > /etc/systemd/system/domainhost.service
systemctl daemon-reload
systemctl enable domainhost.service

echo "[7/7] Start domainhost and verify"
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
