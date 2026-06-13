#!/usr/bin/env bash
# Linux full install: postgres, Technitium DNS, then DomainHost. Windows: install\install.bat.
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
bash "${SCRIPT_DIR}/install-dns.sh"
hephaestus_source_shared_wait
echo "[install] libpam0g (CP /cp login via Linux PAM)"
apt_get install -y libpam0g
bash "${SCRIPT_DIR}/install-data.sh"
bash "${SCRIPT_DIR}/install-soft.sh"
sync
echo "Install finished."
