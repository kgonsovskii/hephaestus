#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

UNIT_NAME=hephaestus-update-once.service

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$0" "$@"
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UPDATE="$SCRIPT_DIR/update.sh"
LOG="${HEPHAESTUS_UPDATE_LOG:-/var/log/hephaestus-update.log}"
export HEPHAESTUS_UPDATE_LOG="$LOG"

mkdir -p "$(dirname "$LOG")" 2>/dev/null || true
touch "$LOG" 2>/dev/null || true

if [ ! -f "$UPDATE" ]; then
  echo "missing $UPDATE" >&2
  exit 1
fi
chmod +x "$UPDATE" 2>/dev/null || true

if systemctl list-unit-files --type=service 2>/dev/null | grep -q '^domainhost\.service'; then
  systemctl disable domainhost 2>/dev/null || true
fi

cat >"/etc/systemd/system/$UNIT_NAME" <<EOF
[Unit]
Description=Run Hephaestus update.sh once after boot
After=network-online.target local-fs.target
Wants=network-online.target

[Service]
Type=oneshot
Environment=HEPHAESTUS_UPDATE_LOG=$LOG
ExecStart=/bin/bash $UPDATE
StandardOutput=append:$LOG
StandardError=append:$LOG
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "$UNIT_NAME"

if ! systemctl reboot --no-block 2>/dev/null; then
  ( sleep 2 && systemctl reboot ) </dev/null >/dev/null 2>&1 &
fi

echo "scheduled $UNIT_NAME; reboot initiated"
