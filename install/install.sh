#!/usr/bin/env bash
# Full stack: Git, .NET 9 SDK, PostgreSQL (hephaestus), Technitium DNS, clone app to /opt/hephaestus.
# Run: sudo bash install/install.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$SCRIPT_DIR/install.sh" "$@"
fi

bash "$SCRIPT_DIR/install-git.sh"
bash "$SCRIPT_DIR/install-net.sh"
bash "$SCRIPT_DIR/install-postgres.sh"
bash "$SCRIPT_DIR/install-dns.sh"
bash "$SCRIPT_DIR/install-soft.sh"
