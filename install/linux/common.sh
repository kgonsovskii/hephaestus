#!/usr/bin/env bash
# Path layout for install/linux/*.sh — source after: SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
[[ -n "${HEPHAESTUS_INSTALL_COMMON_SOURCED:-}" ]] && return 0
HEPHAESTUS_INSTALL_COMMON_SOURCED=1

: "${SCRIPT_DIR:?Set SCRIPT_DIR before sourcing install/linux/common.sh}"

INSTALL_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
REPO_ROOT="$(cd "${INSTALL_ROOT}/.." && pwd)"
SHARED_DIR="${INSTALL_ROOT}/shared"
LINUX_DIR="${SCRIPT_DIR}"
export INSTALL_ROOT REPO_ROOT SHARED_DIR LINUX_DIR

HEPHAESTUS_DATA_DIR_NAME="${HEPHAESTUS_DATA_DIR_NAME:-hephaestus_data}"
export HEPHAESTUS_DATA_DIR_NAME

hephaestus_data_directory() {
  local parent name
  parent="$(dirname "${REPO_ROOT}")"
  name="${HEPHAESTUS_DATA_DIR_NAME}"
  echo "$(cd "${parent}" && pwd)/${name}"
}

hephaestus_source_shared_wait() {
  # shellcheck source=../shared/wait.sh
  . "${SHARED_DIR}/wait.sh"
}
