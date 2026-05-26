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
hephaestus_source_shared_wait

apt_get update
apt_get install -y software-properties-common

if ! dpkg-query -W -f='${Status}' dotnet-sdk-10.0 2>/dev/null | grep -q 'install ok installed'; then
  add-apt-repository -y ppa:dotnet/backports
  apt_get update
fi

apt_get install -y dotnet-sdk-10.0 dotnet-runtime-10.0 aspnetcore-runtime-10.0 curl
# Technitium DNS needs ICU at runtime (https://blog.technitium.com/2017/11/running-dns-server-on-ubuntu-linux.html)
apt_get install -y libicu-dev 2>/dev/null \
  || apt_get install -y libicu74t64 2>/dev/null \
  || apt_get install -y libicu72 2>/dev/null \
  || true

apt_get install -y --only-upgrade \
  dotnet-host \
  dotnet-runtime-10.0 \
  aspnetcore-runtime-10.0 \
  dotnet-sdk-10.0 \
  2>/dev/null || true

if ! dotnet --list-sdks | grep -q '^10\.'; then
  echo ".NET 10 SDK not detected after install (dotnet-sdk-10.0)." >&2
  exit 1
fi

if ! dotnet --info >/dev/null 2>&1; then
  echo "dotnet failed self-check (SDK vs Microsoft.NETCore.App version mismatch is common)." >&2
  echo "Try: sudo apt update && sudo apt install -y --only-upgrade dotnet-host dotnet-runtime-10.0 aspnetcore-runtime-10.0 dotnet-sdk-10.0" >&2
  exit 1
fi

apt_get install -y libmsquic || true
