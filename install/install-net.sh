#!/usr/bin/env bash
# Installs Microsoft .NET 9 SDK (package dotnet-sdk-9.0) on Ubuntu via dotnet/backports PPA,
# plus optional libmsquic for DNS-over-QUIC when building/running Technitium DNS.
# See: https://github.com/TechnitiumSoftware/DnsServer/blob/master/build.md
# Run: sudo bash install/install-net.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

apt-get update
apt-get install -y software-properties-common

if ! dpkg-query -W -f='${Status}' dotnet-sdk-9.0 2>/dev/null | grep -q 'install ok installed'; then
  add-apt-repository -y ppa:dotnet/backports
  apt-get update
fi

apt-get install -y dotnet-sdk-9.0

if ! dotnet --list-sdks | grep -q '^9\.'; then
  echo ".NET 9 SDK not detected after install (dotnet-sdk-9.0)." >&2
  exit 1
fi

# Optional for DNS-over-QUIC / HTTP/3; safe to skip if unavailable
apt-get install -y libmsquic || true
