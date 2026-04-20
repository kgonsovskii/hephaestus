#!/usr/bin/env bash
# Installs Git (and CA certificates for HTTPS remotes). Ubuntu/Debian, non-interactive.
# Run: sudo bash install/install-git.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=wait-for-apt-dpkg-lock.sh
. "$SCRIPT_DIR/wait-for-apt-dpkg-lock.sh"

apt_get update
apt_get install -y git ca-certificates
