#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$SCRIPT_DIR/wait.sh"

apt_get update
apt_get install -y software-properties-common

if ! dpkg-query -W -f='${Status}' dotnet-sdk-9.0 2>/dev/null | grep -q 'install ok installed'; then
  add-apt-repository -y ppa:dotnet/backports
  apt_get update
fi

apt_get install -y dotnet-sdk-9.0 dotnet-runtime-9.0 aspnetcore-runtime-9.0

apt_get install -y --only-upgrade \
  dotnet-host \
  dotnet-runtime-9.0 \
  aspnetcore-runtime-9.0 \
  dotnet-sdk-9.0 \
  2>/dev/null || true

if ! dotnet --list-sdks | grep -q '^9\.'; then
  echo ".NET 9 SDK not detected after install (dotnet-sdk-9.0)." >&2
  exit 1
fi

if ! dotnet --info >/dev/null 2>&1; then
  echo "dotnet failed self-check (SDK vs Microsoft.NETCore.App version mismatch is common)." >&2
  echo "Try: sudo apt update && sudo apt install -y --only-upgrade dotnet-host dotnet-runtime-9.0 aspnetcore-runtime-9.0 dotnet-sdk-9.0" >&2
  exit 1
fi

apt_get install -y libmsquic || true
