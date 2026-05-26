#!/usr/bin/env bash
# Full Linux install (DNS before DomainHost). Delegates to install/linux/install.sh.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "${ROOT}/linux/install.sh" "$@"
