#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "${SCRIPT_DIR}/common.sh"

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$0" "$@"
fi

DATA_DIR="$(hephaestus_data_directory)"

echo "[install-data] Repo root: ${REPO_ROOT}"
echo "[install-data] Data dir (sibling): ${DATA_DIR}"
echo "[install-data] Skipped: hephaestus_data is cloned/synced by DomainHost Git maintenance on start."
