#!/usr/bin/env bash
# SSH to a Linux host: runs install/install-remote.txt on the server (same text as PS1 / InstallRemote).
# No app bundle is copied from the machine running this script.
#
# Usage:
#   install/install-remote.sh
#     → reads install/install-remote-creds.txt (three lines: host, login, password).
#   install/install-remote.sh [server] [login] [password]
#     → overrides creds file per argument; omitted positions use the file.
# Requires: sshpass (e.g. apt install sshpass / brew install sshpass)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CREDS_FILE="${SCRIPT_DIR}/install-remote-creds.txt"

read_install_remote_creds_file() {
  local f="$1"
  if [[ ! -f "$f" ]]; then
    echo "Missing $f — add install/install-remote-creds.txt (three lines: host, login, password)." >&2
    exit 1
  fi
  local -a lines=()
  local line
  while IFS= read -r line || [[ -n "${line:-}" ]]; do
    line="${line//$'\r'/}"
    line="${line#"${line%%[![:space:]]*}"}"
    line="${line%"${line##*[![:space:]]}"}"
    [[ -z "$line" ]] && continue
    [[ "$line" == \#* ]] && continue
    lines+=("$line")
    [[ "${#lines[@]}" -ge 3 ]] && break
  done <"$f"
  if [[ "${#lines[@]}" -lt 3 ]]; then
    echo "$f: need three non-empty, non-comment lines (host, login, password); got ${#lines[@]}." >&2
    exit 1
  fi
  SERVER="${lines[0]}"
  LOGIN="${lines[1]}"
  PASSWORD="${lines[2]}"
}

read_install_remote_creds_file "$CREDS_FILE"
if [[ "$#" -ge 1 ]]; then SERVER="$1"; fi
if [[ "$#" -ge 2 ]]; then LOGIN="$2"; fi
if [[ "$#" -ge 3 ]]; then PASSWORD="$3"; fi

if ! command -v sshpass >/dev/null 2>&1; then
  echo "sshpass is required (SSH password auth). Install: sudo apt install sshpass" >&2
  exit 1
fi

export SSHPASS="$PASSWORD"
SSH_OPTS=(-o StrictHostKeyChecking=accept-new -o ConnectTimeout=30 -o ServerAliveInterval=15 -o ServerAliveCountMax=4)
if [ -n "${SSH_KNOWN_HOSTS:-}" ]; then
  SSH_OPTS+=(-o "UserKnownHostsFile=$SSH_KNOWN_HOSTS")
fi

REMOTE_TXT="${SCRIPT_DIR}/install-remote.txt"

echo "Remote install -> ${LOGIN}@${SERVER}"
echo "[1/1] SSH: install git, clone to \$HOME/hephaestus, run install.sh"

if [ ! -f "$REMOTE_TXT" ]; then
  echo "Missing remote script: $REMOTE_TXT" >&2
  exit 1
fi

sshpass -e ssh -tt "${SSH_OPTS[@]}" "${LOGIN}@${SERVER}" bash -s <"$REMOTE_TXT"
