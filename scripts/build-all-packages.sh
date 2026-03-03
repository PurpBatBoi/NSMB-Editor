#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$ROOT_DIR"

echo "==> Linux AppImage (system Mono mode)"
./scripts/build-linux-appimage.sh

echo "==> Linux AppImage (bundled Mono mode)"
./scripts/build-linux-appimage.sh --bundle-mono

echo "==> Windows ZIP (Any CPU)"
./scripts/build-windows-zip.sh --platform anycpu

echo "==> Done"
ls -lh "$ROOT_DIR/dist" | sed -n '1,120p'
