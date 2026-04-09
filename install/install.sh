#!/usr/bin/env bash
# Full stack: uninstall (app-only), Git, .NET 9 SDK, PostgreSQL (hephaestus), Technitium DNS, DomainHost publish.
# (Clone the repo on the server first, e.g. install-remote.ps1 / install-remote.sh, then run this script.)
# Run: sudo bash install/install.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$SCRIPT_DIR/install.sh" "$@"
fi

bash "$SCRIPT_DIR/uninstall.sh" --for-install
bash "$SCRIPT_DIR/install-git.sh"
bash "$SCRIPT_DIR/install-net.sh"
bash "$SCRIPT_DIR/install-postgres.sh"
bash "$SCRIPT_DIR/install-dns.sh"
bash "$SCRIPT_DIR/install-soft.sh"
