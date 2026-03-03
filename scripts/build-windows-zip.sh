#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
APP_NAME="NSMBe5"
BUILD_CONF="Release"
OUT_DIR="$ROOT_DIR/dist"
PLATFORM_ARG="anycpu"
MSBUILD_PLATFORM="Any CPU"
PUBLISH_DIR="$ROOT_DIR/NSMBe5/bin/$BUILD_CONF"
ZIP_PATH="$OUT_DIR/NSMBe5-windows-anycpu.zip"

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

create_zip() {
  local source_dir="$1"
  local zip_path="$2"

  if command -v zip >/dev/null 2>&1; then
    (
      cd "$source_dir"
      zip -r "$zip_path" .
    )
    return
  fi

  if command -v 7z >/dev/null 2>&1; then
    (
      cd "$source_dir"
      7z a -tzip "$zip_path" . >/dev/null
    )
    return
  fi

  if command -v bsdtar >/dev/null 2>&1; then
    (
      cd "$source_dir"
      bsdtar -a -cf "$zip_path" .
    )
    return
  fi

  echo "Missing required archiver: install zip, 7z, or bsdtar" >&2
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --platform)
      if [[ $# -lt 2 ]]; then
        echo "Missing value for --platform" >&2
        echo "Usage: $0 [--platform anycpu|x86|x64]" >&2
        exit 1
      fi
      PLATFORM_ARG="${2,,}"
      shift 2
      ;;
    *)
      echo "Unknown option: $1" >&2
      echo "Usage: $0 [--platform anycpu|x86|x64]" >&2
      exit 1
      ;;
  esac
done

case "$PLATFORM_ARG" in
  anycpu)
    MSBUILD_PLATFORM="Any CPU"
    PUBLISH_DIR="$ROOT_DIR/NSMBe5/bin/$BUILD_CONF"
    ZIP_PATH="$OUT_DIR/NSMBe5-windows-anycpu.zip"
    ;;
  x86)
    MSBUILD_PLATFORM="x86"
    PUBLISH_DIR="$ROOT_DIR/NSMBe5/bin/x86/$BUILD_CONF"
    ZIP_PATH="$OUT_DIR/NSMBe5-windows-x86.zip"
    ;;
  x64)
    MSBUILD_PLATFORM="x64"
    PUBLISH_DIR="$ROOT_DIR/NSMBe5/bin/x64/$BUILD_CONF"
    ZIP_PATH="$OUT_DIR/NSMBe5-windows-x64.zip"
    ;;
  *)
    echo "Invalid platform: $PLATFORM_ARG" >&2
    echo "Valid values: anycpu, x86, x64" >&2
    exit 1
    ;;
esac

require_cmd msbuild

echo "==> Restoring/building $APP_NAME ($BUILD_CONF, $MSBUILD_PLATFORM)"
msbuild "$ROOT_DIR/NSMBe5.sln" /t:Restore
msbuild "$ROOT_DIR/NSMBe5.sln" /p:Configuration="$BUILD_CONF" /p:Platform="$MSBUILD_PLATFORM"

if [[ ! -f "$PUBLISH_DIR/$APP_NAME.exe" ]]; then
  echo "Build output not found: $PUBLISH_DIR/$APP_NAME.exe" >&2
  exit 1
fi

mkdir -p "$OUT_DIR"
rm -f "$ZIP_PATH"

echo "==> Creating ZIP package"
create_zip "$PUBLISH_DIR" "$ZIP_PATH"

echo "ZIP package created: $ZIP_PATH"
