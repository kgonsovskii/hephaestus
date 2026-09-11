#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "${SCRIPT_DIR}/common.sh"
hephaestus_load_profile_env
hephaestus_source_shared_wait

ensure_pkg curl

if pkg_installed dotnet-sdk-10.0; then
  echo "[net] skip .NET PPA (dotnet-sdk-10.0 already installed)"
else
  ensure_pkg software-properties-common
  add-apt-repository -y ppa:dotnet/backports
  _APT_INDEX_UPDATED=0
  apt_update_once
fi

ensure_pkg dotnet-sdk-10.0
ensure_pkg dotnet-runtime-10.0
ensure_pkg aspnetcore-runtime-10.0

# Technitium DNS needs ICU at runtime (https://blog.technitium.com/2017/11/running-dns-server-on-ubuntu-linux.html)
if pkg_installed libicu-dev || pkg_installed libicu74t64 || pkg_installed libicu72; then
  echo "[apt] skip libicu (already installed)"
else
  apt_update_once
  apt_get install -y libicu-dev 2>/dev/null \
    || apt_get install -y libicu74t64 2>/dev/null \
    || apt_get install -y libicu72 2>/dev/null \
    || true
fi

if ! dotnet --list-sdks | grep -q '^10\.'; then
  echo ".NET 10 SDK not detected after install (dotnet-sdk-10.0)." >&2
  exit 1
fi

if ! dotnet --info >/dev/null 2>&1; then
  echo "dotnet failed self-check (SDK vs Microsoft.NETCore.App version mismatch is common)." >&2
  echo "Try: sudo apt update && sudo apt install -y --only-upgrade dotnet-host dotnet-runtime-10.0 aspnetcore-runtime-10.0 dotnet-sdk-10.0" >&2
  exit 1
fi

if pkg_installed libmsquic; then
  echo "[apt] skip libmsquic (already installed)"
else
  apt_update_once
  apt_get install -y libmsquic || true
fi
