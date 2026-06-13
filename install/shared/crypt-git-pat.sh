#!/usr/bin/env bash
# Hephaestus Git PAT obfuscation (XOR + hex). Not strong crypto — keeps github_pat_* out of git plaintext.
set -euo pipefail

HEPHAESTUS_GIT_PAT_KEY='HephaestusGitKey42'

git_pat_encrypted_blob_path() {
  local script_dir
  script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  printf '%s/git-pat.enc' "${script_dir}"
}

encrypt_git_pat() {
  local plain="$1"
  local key="${HEPHAESTUS_GIT_PAT_KEY}"
  local key_len=${#key}
  local i c k xor hex=''
  for ((i = 0; i < ${#plain}; i++)); do
    c="${plain:i:1}"
    k="${key:i % key_len:1}"
    xor=$(( $(LC_ALL=C printf '%d' "'$c") ^ $(LC_ALL=C printf '%d' "'$k") ))
    hex+=$(printf '%02x' "${xor}")
  done
  printf '%s' "${hex}"
}

decrypt_git_pat() {
  local enc="$1"
  local key="${HEPHAESTUS_GIT_PAT_KEY}"
  local key_len=${#key}
  local hex i b k xor plain=''
  hex="$(printf '%s' "${enc}" | tr -d '[:space:]')"
  if [ -z "${hex}" ] || [ $(( ${#hex} % 2 )) -ne 0 ]; then
    echo "Encrypted Git PAT hex is empty or has odd length." >&2
    return 1
  fi
  for ((i = 0; i < ${#hex}; i += 2)); do
    b=$(( 16#${hex:i:2} ))
    k="${key:i / 2 % key_len:1}"
    xor=$(( b ^ $(LC_ALL=C printf '%d' "'$k") ))
    plain+=$(printf "\\$(printf '%03o' "${xor}")")
  done
  printf '%s' "${plain}"
}

read_git_pat_from_encrypted_file() {
  local path
  path="$(git_pat_encrypted_blob_path)"
  if [ ! -f "${path}" ]; then
    echo "Encrypted PAT file not found: ${path}" >&2
    return 1
  fi
  decrypt_git_pat "$(tr -d '[:space:]' < "${path}")"
}

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
  action="${1:-}"
  case "${action}" in
    encrypt)
      [ -n "${2:-}" ] || { echo "usage: $0 encrypt <token>" >&2; exit 1; }
      encrypt_git_pat "$2"
      ;;
    decrypt)
      [ -n "${2:-}" ] || { echo "usage: $0 decrypt <hex>" >&2; exit 1; }
      decrypt_git_pat "$2"
      ;;
    show)
      read_git_pat_from_encrypted_file
      ;;
    *)
      echo "usage: $0 encrypt|decrypt|show [value]" >&2
      exit 1
      ;;
  esac
fi
