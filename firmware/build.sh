#!/usr/bin/env bash
# Compile the DrumDuino firmware with arduino-cli.
#
# The sketch is split across several .ino files whose main file
# (microdrum.ino) does not match this folder name, so arduino-cli cannot
# compile firmware/ directly. This script assembles a valid sketch in a
# temporary directory (main file named after the folder) and compiles it.
set -euo pipefail

FQBN="${FQBN:-arduino:avr:mega}"
FW_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MAIN_INO="microdrum.ino"

if ! command -v arduino-cli >/dev/null 2>&1; then
  echo "error: arduino-cli not found on PATH. Run .cursor/install.sh first." >&2
  exit 1
fi

BUILD_ROOT="$(mktemp -d)"
SKETCH_NAME="$(basename "${MAIN_INO%.ino}")"
SKETCH_DIR="${BUILD_ROOT}/${SKETCH_NAME}"
mkdir -p "$SKETCH_DIR"
trap 'rm -rf "$BUILD_ROOT"' EXIT

cp "$FW_DIR"/*.ino "$SKETCH_DIR"/
cp "$FW_DIR"/*.h "$FW_DIR"/*.cpp "$SKETCH_DIR"/ 2>/dev/null || true

echo "Compiling firmware for ${FQBN}..."
arduino-cli compile --fqbn "$FQBN" "$SKETCH_DIR" "$@"
