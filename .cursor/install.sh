#!/usr/bin/env bash
# Idempotent development environment setup for DrumDuino.
#
# Installs the Arduino toolchain (arduino-cli + AVR core) required to build
# the firmware in firmware/. Safe to run repeatedly.
set -euo pipefail

ARDUINO_CLI_VERSION="1.5.1"
CORE="arduino:avr"

install_arduino_cli() {
  if command -v arduino-cli >/dev/null 2>&1; then
    echo "arduino-cli already installed: $(arduino-cli version)"
    return
  fi
  echo "Installing arduino-cli ${ARDUINO_CLI_VERSION}..."
  local tmp
  tmp="$(mktemp -d)"
  curl -fsSL \
    "https://downloads.arduino.cc/arduino-cli/arduino-cli_${ARDUINO_CLI_VERSION}_Linux_64bit.tar.gz" \
    -o "${tmp}/arduino-cli.tar.gz"
  tar -xzf "${tmp}/arduino-cli.tar.gz" -C "${tmp}" arduino-cli
  if command -v sudo >/dev/null 2>&1 && sudo -n true 2>/dev/null; then
    sudo install -m 0755 "${tmp}/arduino-cli" /usr/local/bin/arduino-cli
  else
    mkdir -p "${HOME}/.local/bin"
    install -m 0755 "${tmp}/arduino-cli" "${HOME}/.local/bin/arduino-cli"
    case ":${PATH}:" in
      *":${HOME}/.local/bin:"*) ;;
      *) echo 'export PATH="$HOME/.local/bin:$PATH"' >> "${HOME}/.bashrc" ;;
    esac
    export PATH="${HOME}/.local/bin:${PATH}"
  fi
  rm -rf "${tmp}"
  echo "Installed: $(arduino-cli version)"
}

install_core() {
  echo "Updating Arduino package index..."
  arduino-cli core update-index
  echo "Installing ${CORE} core..."
  arduino-cli core install "${CORE}"
}

install_arduino_cli
install_core

echo "Development environment ready."
echo "Build the firmware with: firmware/build.sh"
