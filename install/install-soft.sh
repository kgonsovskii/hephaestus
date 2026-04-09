#!/usr/bin/env bash
# Replaces /opt/hephaestus with a fresh clone of the Hephaestus repo (requires git).
# Run: sudo bash install/install-soft.sh
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

if ! command -v git >/dev/null 2>&1; then
  echo "git not found. Run install/install-git.sh first (or install/install.sh)." >&2
  exit 1
fi

readonly HEPH_DIR=/opt/hephaestus
readonly HEPH_REPO=https://github.com/kgonsovskii/hephaestus.git

rm -rf "$HEPH_DIR"
git clone --depth 1 "$HEPH_REPO" "$HEPH_DIR"
