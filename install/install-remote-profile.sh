#!/usr/bin/env bash
set -euo pipefail
read -rp "Hephaestus profile: " profile
profile="${profile#"${profile%%[![:space:]]*}"}"
profile="${profile%"${profile##*[![:space:]]}"}"
if [[ -z "${profile}" ]]; then
  echo "Profile is required." >&2
  exit 1
fi
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "${ROOT}/install-remote.sh" "${profile}" "$@"
