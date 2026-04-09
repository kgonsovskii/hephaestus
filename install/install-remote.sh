#!/usr/bin/env bash
# Uploads this install/ directory to the server over SSH and runs install.sh remotely.
# Requires: sshpass (e.g. apt install sshpass / brew install sshpass)
#
# Usage: install/install-remote.sh [server] [login] [password]
# Defaults: 216.203.21.239 root 1!Ogviobhuetly
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly REMOTE_DIR=/tmp/hephaestus-install

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
SSH_OPTS=(-o StrictHostKeyChecking=accept-new)
if [ -n "${SSH_KNOWN_HOSTS:-}" ]; then
  SSH_OPTS+=(-o "UserKnownHostsFile=$SSH_KNOWN_HOSTS")
fi

echo "Remote install -> ${LOGIN}@${SERVER} (${REMOTE_DIR})"

tar czf - -C "$SCRIPT_DIR" . | sshpass -e ssh "${SSH_OPTS[@]}" "${LOGIN}@${SERVER}" \
  "rm -rf ${REMOTE_DIR} && mkdir -p ${REMOTE_DIR} && tar xzf - -C ${REMOTE_DIR}"

sshpass -e ssh "${SSH_OPTS[@]}" "${LOGIN}@${SERVER}" "bash ${REMOTE_DIR}/install.sh"
