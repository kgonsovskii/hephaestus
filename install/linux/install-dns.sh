#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "${SCRIPT_DIR}/common.sh"

hephaestus_load_profile_env

readonly INSTALL_PROJ="${INSTALL_ROOT}/Install/Install.csproj"
readonly TECHNI_ROOT=/opt/technitium
readonly INSTALL_DIR="${TECHNI_ROOT}/dns"

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

hephaestus_source_shared_wait
if ! command -v curl >/dev/null 2>&1; then
  apt_get install -y curl
fi

if ! command -v git >/dev/null 2>&1; then
  echo "git not found. Run install/linux/install-git.sh first (or install/install.sh)." >&2
  exit 1
fi
if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Run install/linux/install-net.sh first (or install/install.sh)." >&2
  exit 1
fi

if ! dotnet --info >/dev/null 2>&1; then
  echo "dotnet is not runnable on this host (often SDK needs a newer Microsoft.NETCore.App than installed)." >&2
  echo "Fix: sudo apt update && sudo apt install -y --only-upgrade dotnet-host dotnet-runtime-10.0 aspnetcore-runtime-10.0 dotnet-sdk-10.0" >&2
  echo "Or re-run: sudo bash install/linux/install-net.sh" >&2
  exit 1
fi

if [ ! -f "$INSTALL_PROJ" ]; then
  echo "Missing install project (expected Commons-linked appsettings): $INSTALL_PROJ" >&2
  exit 1
fi

echo "[dns 1] Build hephaestus-install (Technitium password from panel/Commons/appsettings.json)"
dotnet build "$INSTALL_PROJ" -c Release -v minimal

readonly BUILD_DIR="${TECHNI_ROOT}/build"

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

# https://blog.technitium.com/2017/11/running-dns-server-on-ubuntu-linux.html (manual systemd steps)
setup_technitium_service_account() {
  mkdir -p /etc/dns /var/log/technitium/dns
  if ! id dns-server >/dev/null 2>&1; then
    echo "[dns] useradd dns-server (Technitium manual install)"
    useradd --system -M --shell /usr/sbin/nologin dns-server
  fi
  if [ -d "${INSTALL_DIR}/config" ]; then
    shopt -s dotglob nullglob
    local cfg=( "${INSTALL_DIR}/config"/* )
    if [ "${#cfg[@]}" -gt 0 ]; then
      echo "[dns] Copy packaged config -> /etc/dns"
      cp -a "${INSTALL_DIR}/config"/* /etc/dns/
    fi
  fi
  chown -R dns-server:dns-server "${INSTALL_DIR}" /etc/dns /var/log/technitium/dns
}

wait_for_technitium_dns() {
  local deadline=$((SECONDS + 90))
  while [ "$SECONDS" -lt "$deadline" ]; do
    if systemctl is-active --quiet dns.service \
        && curl -fsS --max-time 3 http://127.0.0.1:5380/ >/dev/null 2>&1; then
      return 0
    fi
    sleep 2
  done
  echo "[dns] Technitium did not become ready on http://127.0.0.1:5380 within 90s" >&2
  systemctl status dns.service --no-pager >&2 || true
  journalctl -u dns.service -n 25 --no-pager >&2 || true
  return 1
}

set_local_resolver_loopback() {
  local backup=/etc/resolv.conf.hephaestus.bak
  if [ ! -f "$backup" ]; then
    cp -a /etc/resolv.conf "$backup" 2>/dev/null || true
  fi
  printf 'nameserver 127.0.0.1\n' >/etc/resolv.conf
  echo "[dns] /etc/resolv.conf -> 127.0.0.1 (backup: $backup)"
}

restore_resolver_fallback() {
  local backup=/etc/resolv.conf.hephaestus.bak
  if [ -f "$backup" ] && ! grep -q '^nameserver 127\.0\.0\.1$' "$backup" 2>/dev/null; then
    cp -a "$backup" /etc/resolv.conf
    echo "[dns] Restored /etc/resolv.conf from backup."
  else
    printf 'nameserver 8.8.8.8\nnameserver 1.1.1.1\n' >/etc/resolv.conf
    echo "[dns] Restored /etc/resolv.conf to public DNS (8.8.8.8, 1.1.1.1)."
  fi
}

if [ -f "${INSTALL_DIR}/systemd.service" ]; then
  cp "${INSTALL_DIR}/systemd.service" /etc/systemd/system/dns.service
  systemctl daemon-reload
fi

setup_technitium_service_account

systemctl stop systemd-resolved 2>/dev/null || true
systemctl disable systemd-resolved 2>/dev/null || true

systemctl enable dns.service
if ! systemctl restart dns.service; then
  restore_resolver_fallback
  exit 1
fi

if ! wait_for_technitium_dns; then
  restore_resolver_fallback
  exit 1
fi

set_local_resolver_loopback

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
