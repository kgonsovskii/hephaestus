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

FORCE_DNS=0
PROFILE_ARG=""
for arg in "$@"; do
  case "$arg" in
    --force) FORCE_DNS=1 ;;
    -*)
      echo "Unknown option: $arg (supported: --force)" >&2
      exit 1
      ;;
    *) PROFILE_ARG="$arg" ;;
  esac
done

hephaestus_bootstrap_profile "${PROFILE_ARG}"

bash "${SCRIPT_DIR}/uninstall.sh"
bash "${SCRIPT_DIR}/install-git.sh"
bash "${SCRIPT_DIR}/install-net.sh"
bash "${SCRIPT_DIR}/install-postgres.sh"
if [ "$FORCE_DNS" -eq 1 ]; then
  bash "${SCRIPT_DIR}/install-dns.sh" --force
else
  bash "${SCRIPT_DIR}/install-dns.sh"
fi
hephaestus_source_shared_wait
echo "[install] libpam0g (CP /cp login via Linux PAM)"
ensure_pkg libpam0g
bash "${SCRIPT_DIR}/install-data.sh"
bash "${SCRIPT_DIR}/install-soft.sh"
sync
echo "Install finished."
