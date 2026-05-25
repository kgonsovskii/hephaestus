#!/usr/bin/env bash
# Ensure every *.sh in this repo is marked executable in git (100755). Run from repo root.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "${SCRIPT_DIR}/common.sh"
ROOT="${REPO_ROOT}"
cd "$ROOT"

while IFS= read -r -d '' f; do
  rel="${f#./}"
  if git ls-files --error-unmatch "$rel" >/dev/null 2>&1; then
    git update-index --chmod=+x "$rel"
  else
    git add --chmod=+x "$rel"
  fi
done < <(find . -name '*.sh' -not -path './.git/*' -print0)

echo "Git executable bit set for:"
git ls-files -s '*.sh' | awk '{ print $4, $1 }'
