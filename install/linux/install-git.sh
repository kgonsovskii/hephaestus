#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "${SCRIPT_DIR}/common.sh"
hephaestus_load_profile_env
hephaestus_source_shared_wait

apt_get update
apt_get install -y git ca-certificates
