#!/usr/bin/env bash
# SSH to a Linux host: runs install/install-remote.txt on the server (same text as PS1 / InstallRemote).
# No app bundle is copied from the machine running this script.
#
# Usage: install/install-remote.sh [server] [login] [password]
# Defaults: 216.203.21.239 root 1!Ogviobhuetly
# Requires: sshpass (e.g. apt install sshpass / brew install sshpass)
set -euo pipefail

readonly DEFAULT_SERVER=216.203.21.239
readonly DEFAULT_LOGIN=root
readonly DEFAULT_PASS='1!Ogviobhuetly'

SERVER="${1:-$DEFAULT_SERVER}"
LOGIN="${2:-$DEFAULT_LOGIN}"
PASSWORD="${3:-$DEFAULT_PASS}"

if ! command -v sshpass >/dev/null 2>&1; then
  echo "sshpass is required (SSH password auth). Install: sudo apt install sshpass" >&2
  exit 1
fi

export SSHPASS="$PASSWORD"
SSH_OPTS=(-o StrictHostKeyChecking=accept-new -o ConnectTimeout=30 -o ServerAliveInterval=15 -o ServerAliveCountMax=4)
if [ -n "${SSH_KNOWN_HOSTS:-}" ]; then
  SSH_OPTS+=(-o "UserKnownHostsFile=$SSH_KNOWN_HOSTS")
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REMOTE_TXT="${SCRIPT_DIR}/install-remote.txt"

echo "Remote install -> ${LOGIN}@${SERVER}"
echo "[1/1] SSH: install git, clone to \$HOME/hephaestus, run install.sh"

if [ ! -f "$REMOTE_TXT" ]; then
  echo "Missing remote script: $REMOTE_TXT" >&2
  exit 1
fi

sshpass -e ssh -tt "${SSH_OPTS[@]}" "${LOGIN}@${SERVER}" bash -s <"$REMOTE_TXT"
