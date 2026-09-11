#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "${SCRIPT_DIR}/common.sh"

CREDS_FILE="${SHARED_DIR}/install-remote-creds.txt"

FORCE_DNS=0
FILTERED_ARGS=()
for arg in "$@"; do
  if [ "$arg" = "--force" ]; then
    FORCE_DNS=1
  else
    FILTERED_ARGS+=("$arg")
  fi
done
if ((${#FILTERED_ARGS[@]})); then
  set -- "${FILTERED_ARGS[@]}"
else
  set --
fi

read_install_remote_creds_file() {
  local f="$1"
  if [[ ! -f "$f" ]]; then
    echo "Missing $f - add install/shared/install-remote-creds.txt (host, login, password, profile)." >&2
    exit 1
  fi
  SERVERS=()
  LOGINS=()
  PASSWORDS=()
  PROFILES=()
  local -a lines=()
  local line
  while IFS= read -r line || [[ -n "${line:-}" ]]; do
    line="${line//$'\r'/}"
    line="${line#"${line%%[![:space:]]*}"}"
    line="${line%"${line##*[![:space:]]}"}"
    [[ -z "$line" ]] && continue
    [[ "$line" == \#* ]] && continue
    lines+=("$line")
  done <"$f"
  if [[ "${#lines[@]}" -eq 0 || $(( ${#lines[@]} % 4 )) -ne 0 ]]; then
    echo "$f: need one or more host/login/password/profile quadruplets; got ${#lines[@]} line(s)." >&2
    exit 1
  fi
  local i
  for ((i = 0; i < ${#lines[@]}; i += 4)); do
    hephaestus_validate_profile_name "${lines[i + 3]}" || exit 1
    SERVERS+=("${lines[i]}")
    LOGINS+=("${lines[i + 1]}")
    PASSWORDS+=("${lines[i + 2]}")
    PROFILES+=("${HEPHAESTUS_PROFILE}")
  done
}

if [[ $# -ge 1 ]]; then
  hephaestus_write_profile_file "$1"
  shift
fi
hephaestus_load_profile_env
CLI_PROFILE="${HEPHAESTUS_PROFILE}"

read_install_remote_creds_file "$CREDS_FILE"
if [[ "$#" -ge 1 ]]; then
  local_login="${LOGINS[0]}"
  local_password="${PASSWORDS[0]}"
  SERVERS=("$1")
  if [[ "$#" -ge 2 ]]; then local_login="$2"; fi
  if [[ "$#" -ge 3 ]]; then local_password="$3"; fi
  LOGINS=("$local_login")
  PASSWORDS=("$local_password")
  PROFILES=("${CLI_PROFILE}")
fi

if ! command -v sshpass >/dev/null 2>&1; then
  echo "[install-remote] sshpass not found; installing via apt…"
  hephaestus_source_shared_wait
  ensure_pkg sshpass
fi

SSH_OPTS=(-o StrictHostKeyChecking=accept-new -o ConnectTimeout=30 -o ServerAliveInterval=15 -o ServerAliveCountMax=4)
if [ -n "${SSH_KNOWN_HOSTS:-}" ]; then
  SSH_OPTS+=(-o "UserKnownHostsFile=$SSH_KNOWN_HOSTS")
fi

REMOTE_TXT="${SHARED_DIR}/install-remote.txt"
WAIT_SH="${SHARED_DIR}/wait.sh"

if [ ! -f "$REMOTE_TXT" ]; then
  echo "Missing remote script: $REMOTE_TXT" >&2
  exit 1
fi
if [ ! -f "$WAIT_SH" ]; then
  echo "Missing: $WAIT_SH" >&2
  exit 1
fi

echo "Remote install: ${#SERVERS[@]} server(s) in parallel (profile from creds, overwritten on each target)"
for ((i = 0; i < ${#SERVERS[@]}; i++)); do
  echo "  - ${LOGINS[i]}@${SERVERS[i]}  profile ${PROFILES[i]}"
done
echo "SSH: write \$HOME/profile.txt, clone to \$HOME/hephaestus, run install.sh"

WORKDIR="$(mktemp -d)"
cleanup() { rm -rf "$WORKDIR"; }
trap cleanup EXIT

run_one() {
  local idx="$1"
  local server="${SERVERS[idx]}"
  local login="${LOGINS[idx]}"
  local password="${PASSWORDS[idx]}"
  local profile="${PROFILES[idx]}"
  local exitfile="${WORKDIR}/${idx}.exit"
  local profile_export="export HEPHAESTUS_PROFILE='${profile//\'/\'\\\'\'}'; export HEPHAESTUS_FORCE_DNS='${FORCE_DNS}'"
  (
    export SSHPASS="$password"
    set +e
    { printf '%s\n' "$profile_export"; cat "$WAIT_SH" "$REMOTE_TXT"; } \
      | sshpass -e ssh -tt "${SSH_OPTS[@]}" "${login}@${server}" bash -s 2>&1 \
      | sed -u "s/^/[${server}] /"
    echo "${PIPESTATUS[1]}" >"$exitfile"
  )
}

pids=()
for ((i = 0; i < ${#SERVERS[@]}; i++)); do
  run_one "$i" &
  pids+=("$!")
done

set +e
for pid in "${pids[@]}"; do
  wait "$pid"
done
set -e

echo
echo "=== Remote install report ==="
fail=0
ok=0
for ((i = 0; i < ${#SERVERS[@]}; i++)); do
  code="1"
  if [[ -f "${WORKDIR}/${i}.exit" ]]; then
    code="$(tr -d '[:space:]' <"${WORKDIR}/${i}.exit")"
  fi
  if [[ "$code" == "0" ]]; then
    echo "  OK    ${LOGINS[i]}@${SERVERS[i]}  profile ${PROFILES[i]}"
    ok=$((ok + 1))
  else
    echo "  FAIL  ${LOGINS[i]}@${SERVERS[i]}  profile ${PROFILES[i]}  exit ${code}"
    fail=$((fail + 1))
  fi
done
echo "${#SERVERS[@]} server(s): ${ok} succeeded, ${fail} failed"

if [[ "$fail" -ne 0 ]]; then
  exit 1
fi
