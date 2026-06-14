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

hephaestus_profile_file_path() {
  local parent
  parent="$(dirname "${REPO_ROOT}")"
  echo "$(cd "${parent}" && pwd)/profile.txt"
}

hephaestus_validate_profile_name() {
  local name="${1#"${1%%[![:space:]]*}"}"
  name="${name%"${name##*[![:space:]]}"}"
  if [[ -z "${name}" || "${name}" == "." || "${name}" == ".." || "${name}" == */* || "${name}" == *\\* ]]; then
    echo "Invalid profile name: '${1}'" >&2
    return 1
  fi
  HEPHAESTUS_PROFILE="${name}"
  export HEPHAESTUS_PROFILE
}

hephaestus_write_profile_file() {
  local name="$1"
  hephaestus_validate_profile_name "${name}" || return 1
  local path
  path="$(hephaestus_profile_file_path)"
  printf '%s\n' "${HEPHAESTUS_PROFILE}" >"${path}"
  echo "[install] Wrote profile '${HEPHAESTUS_PROFILE}' to ${path}"
}

hephaestus_apply_profile_arg() {
  if [[ -n "${1:-}" ]]; then
    hephaestus_write_profile_file "$1"
  fi
}

hephaestus_load_profile_env() {
  if [[ -n "${HEPHAESTUS_PROFILE:-}" ]]; then
    hephaestus_validate_profile_name "${HEPHAESTUS_PROFILE}"
    return
  fi
  local path line
  path="$(hephaestus_profile_file_path)"
  if [[ -f "$path" ]]; then
    line="$(head -n1 "$path" | tr -d '\r')"
    line="${line#"${line%%[![:space:]]*}"}"
    line="${line%"${line##*[![:space:]]}"}"
    if [[ -n "$line" ]]; then
      hephaestus_validate_profile_name "$line"
      return
    fi
  fi
  HEPHAESTUS_PROFILE="default"
  export HEPHAESTUS_PROFILE
}

hephaestus_bootstrap_profile() {
  hephaestus_apply_profile_arg "${1:-}"
  hephaestus_load_profile_env
}
