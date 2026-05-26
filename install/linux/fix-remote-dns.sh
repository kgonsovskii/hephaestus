#!/usr/bin/env bash
# One-shot repair: restore resolver, create dns-server + /etc/dns, start Technitium, then loopback DNS.
set -eu

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$0" "$@"
fi

readonly INSTALL_DIR=/opt/technitium/dns

echo "[fix] Temporary public DNS so apt/git work..."
printf 'nameserver 8.8.8.8\nnameserver 1.1.1.1\n' >/etc/resolv.conf

mkdir -p /etc/dns /var/log/technitium/dns
if ! id dns-server >/dev/null 2>&1; then
  echo "[fix] useradd dns-server (Technitium manual install)"
  useradd --system -M --shell /usr/sbin/nologin dns-server
fi
if [ -d "${INSTALL_DIR}/config" ]; then
  shopt -s dotglob nullglob
  cfg=( "${INSTALL_DIR}/config"/* )
  if [ "${#cfg[@]}" -gt 0 ]; then
    echo "[fix] Copy config -> /etc/dns"
    cp -a "${INSTALL_DIR}/config"/* /etc/dns/
  fi
fi

chown -R dns-server:dns-server "${INSTALL_DIR}" /etc/dns /var/log/technitium/dns

systemctl daemon-reload
systemctl enable dns.service
systemctl restart dns.service

echo "[fix] Waiting for Technitium HTTP on :5380..."
deadline=$((SECONDS + 90))
while [ "$SECONDS" -lt "$deadline" ]; do
  if systemctl is-active --quiet dns.service \
      && curl -fsS --max-time 3 http://127.0.0.1:5380/ >/dev/null 2>&1; then
    echo "[fix] Technitium is up."
    break
  fi
  sleep 2
done

if ! systemctl is-active --quiet dns.service; then
  echo "[fix] dns.service not active:" >&2
  systemctl status dns.service --no-pager >&2 || true
  journalctl -u dns.service -n 30 --no-pager >&2 || true
  exit 1
fi

if ! curl -fsS --max-time 5 http://127.0.0.1:5380/ >/dev/null 2>&1; then
  echo "[fix] Port 5380 not responding." >&2
  exit 1
fi

echo "[fix] Point resolver at local Technitium..."
printf 'nameserver 127.0.0.1\n' >/etc/resolv.conf
echo "[fix] Done. Web: http://$(hostname -f 2>/dev/null || hostname):5380/"
