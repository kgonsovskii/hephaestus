#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "${SCRIPT_DIR}/common.sh"

if [ "${EUID:-0}" -ne 0 ]; then
  exec sudo /usr/bin/env bash "$0" "$@"
fi

if ! command -v git >/dev/null 2>&1; then
  echo "git not found. Run install/linux/install-git.sh first." >&2
  exit 1
fi

DATA_DIR="$(hephaestus_data_directory)"
# shellcheck source=../shared/crypt-git-pat.sh
. "${SCRIPT_DIR}/../shared/crypt-git-pat.sh"
PAT="$(read_git_pat_from_encrypted_file)"
CLONE_URL="https://x-access-token:${PAT}@github.com/kgonsovskii/hephaestus_data.git"

echo "[install-data] Repo root: ${REPO_ROOT}"
echo "[install-data] Data dir (sibling): ${DATA_DIR}"
echo "[install-data] Remove existing ${DATA_DIR}"
rm -rf "${DATA_DIR}"

echo "[install-data] Clone https://github.com/kgonsovskii/hephaestus_data.git"
mkdir -p "$(dirname "${DATA_DIR}")"
git clone "${CLONE_URL}" "${DATA_DIR}"

echo "[install-data] Done."
