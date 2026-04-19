#!/usr/bin/env bash
# Installs Git (and CA certificates for HTTPS remotes). Ubuntu/Debian, non-interactive.
# Run: sudo bash install/install-git.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

apt-get update
apt-get install -y git ca-certificates
