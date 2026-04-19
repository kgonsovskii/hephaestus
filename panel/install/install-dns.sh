#!/usr/bin/env bash
# Installs Technitium DNS Server from source on Ubuntu (non-interactive).
# Based on: https://github.com/TechnitiumSoftware/DnsServer/blob/master/build.md
#
# First builds install/Install (links src/Commons/appsettings.json for Technitium:Password), then after dns.service
# is up runs `dotnet run` on that project to set the admin password via HTTP API (no web UI prompt).
#
# Prerequisites: install-git.sh (git) and install-net.sh (dotnet SDK), or run install.sh.
# Run from repo root: sudo bash install/install-dns.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
readonly INSTALL_PROJ="$REPO_ROOT/install/Install/Install.csproj"

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

if ! command -v git >/dev/null 2>&1; then
  echo "git not found. Run install/install-git.sh first (or install/install.sh)." >&2
  exit 1
fi
if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Run install/install-net.sh first (or install/install.sh)." >&2
  exit 1
fi

if [ ! -f "$INSTALL_PROJ" ]; then
  echo "Missing install project (expected Commons-linked appsettings): $INSTALL_PROJ" >&2
  exit 1
fi

echo "[dns 1] Build hephaestus-install (Technitium password from src/Commons/appsettings.json)"
dotnet build "$INSTALL_PROJ" -c Release -v minimal

readonly TECHNI_ROOT=/opt/technitium
readonly BUILD_DIR="${TECHNI_ROOT}/build"
readonly INSTALL_DIR="${TECHNI_ROOT}/dns"

mkdir -p "${BUILD_DIR}"
cd "${BUILD_DIR}"

if [ ! -d TechnitiumLibrary/.git ]; then
  rm -rf TechnitiumLibrary
  git clone --depth 1 https://github.com/TechnitiumSoftware/TechnitiumLibrary.git TechnitiumLibrary
fi
if [ ! -d DnsServer/.git ]; then
  rm -rf DnsServer
  git clone --depth 1 https://github.com/TechnitiumSoftware/DnsServer.git DnsServer
fi

dotnet build TechnitiumLibrary/TechnitiumLibrary.ByteTree/TechnitiumLibrary.ByteTree.csproj -c Release
dotnet build TechnitiumLibrary/TechnitiumLibrary.Net/TechnitiumLibrary.Net.csproj -c Release
dotnet build TechnitiumLibrary/TechnitiumLibrary.Security.OTP/TechnitiumLibrary.Security.OTP.csproj -c Release

dotnet publish DnsServer/DnsServerApp/DnsServerApp.csproj -c Release

PUBLISH=""
for d in DnsServer/DnsServerApp/bin/Release/*/publish; do
  if [ -d "$d" ]; then PUBLISH=$d; break; fi
done
if [ -z "$PUBLISH" ] && [ -d DnsServer/DnsServerApp/bin/Release/publish ]; then
  PUBLISH=DnsServer/DnsServerApp/bin/Release/publish
fi
if [ -z "$PUBLISH" ] || [ ! -d "$PUBLISH" ]; then
  echo "Could not find publish output under DnsServer/DnsServerApp/bin/Release" >&2
  exit 1
fi

mkdir -p "${INSTALL_DIR}"
shopt -s dotglob
cp -a "${PUBLISH}"/* "${INSTALL_DIR}/"

if [ -f "${INSTALL_DIR}/systemd.service" ]; then
  cp "${INSTALL_DIR}/systemd.service" /etc/systemd/system/dns.service
  systemctl daemon-reload
fi

systemctl stop systemd-resolved 2>/dev/null || true
systemctl disable systemd-resolved 2>/dev/null || true

systemctl enable dns.service
systemctl restart dns.service

rm -f /etc/resolv.conf
echo "nameserver 127.0.0.1" > /etc/resolv.conf

echo "[dns] Apply Technitium admin password (Commons appsettings via install/Install)"
set +e
dotnet run --project "$INSTALL_PROJ" -c Release -v minimal
_install_pw=$?
set -e
if [ "$_install_pw" -ne 0 ]; then
  echo "WARNING: hephaestus-install exited ${_install_pw}; set Technitium admin password in web UI if needed." >&2
fi

echo "Technitium DNS Server installed under ${INSTALL_DIR}."
echo "Web console: http://<this-host>:5380/"
echo "See also: https://github.com/TechnitiumSoftware/DnsServer/blob/master/build.md"
