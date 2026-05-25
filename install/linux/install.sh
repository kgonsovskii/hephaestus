#!/usr/bin/env bash
# Linux full install. Windows: install\install.bat (Administrator, Chocolatey).
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "${SCRIPT_DIR}/common.sh"

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "${SCRIPT_DIR}/install.sh" "$@"
fi

bash "${SCRIPT_DIR}/uninstall.sh"
bash "${SCRIPT_DIR}/install-git.sh"
bash "${SCRIPT_DIR}/install-net.sh"
bash "${SCRIPT_DIR}/install-postgres.sh"
bash "${SCRIPT_DIR}/install-soft.sh"
bash "${SCRIPT_DIR}/install-dns.sh"
sync
echo "Install finished."
