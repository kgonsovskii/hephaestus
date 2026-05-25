#!/usr/bin/env bash
# Entry point: delegates to install/linux/install.sh (same as full Linux install).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "${ROOT}/linux/install.sh" "$@"
