#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
APP_NAME="NSMBe5"
APPDIR="$ROOT_DIR/dist/AppDir"
OUT_DIR="$ROOT_DIR/dist"
BUILD_CONF="Release"
PUBLISH_DIR="$ROOT_DIR/NSMBe5/bin/$BUILD_CONF"
BUNDLE_MONO=0

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

for arg in "$@"; do
  case "$arg" in
    --bundle-mono)
      BUNDLE_MONO=1
      ;;
    *)
      echo "Unknown option: $arg" >&2
      echo "Usage: $0 [--bundle-mono]" >&2
      exit 1
      ;;
  esac
done

bundle_mono_runtime() {
  local mono_bin mono_prefix mono_etc
  mono_bin="$(command -v mono)"
  mono_prefix="$(cd -- "$(dirname -- "$mono_bin")/.." && pwd)"

  mkdir -p "$APPDIR/usr/lib"
  mkdir -p "$APPDIR/usr/etc"

  cp -L "$mono_bin" "$APPDIR/usr/bin/mono"

  if [[ ! -d "$mono_prefix/lib/mono" ]]; then
    echo "Could not find mono runtime directory at $mono_prefix/lib/mono" >&2
    exit 1
  fi
  cp -a "$mono_prefix/lib/mono" "$APPDIR/usr/lib/"

  mono_etc="$mono_prefix/etc/mono"
  if [[ -d /etc/mono ]]; then
    cp -a /etc/mono "$APPDIR/usr/etc/"
  elif [[ -d "$mono_etc" ]]; then
    cp -a "$mono_etc" "$APPDIR/usr/etc/"
  fi

  shopt -s nullglob
  for so in "$mono_prefix"/lib/libmono*.so*; do
    cp -a "$so" "$APPDIR/usr/lib/"
  done
  for so in "$mono_prefix"/lib/libMono*.so*; do
    cp -a "$so" "$APPDIR/usr/lib/"
  done
  shopt -u nullglob
}

require_cmd msbuild
require_cmd appimagetool

echo "==> Restoring/building $APP_NAME ($BUILD_CONF)"
msbuild "$ROOT_DIR/NSMBe5.sln" /t:Restore
msbuild "$ROOT_DIR/NSMBe5.sln" /p:Configuration="$BUILD_CONF"

if [[ ! -f "$PUBLISH_DIR/$APP_NAME.exe" ]]; then
  echo "Build output not found: $PUBLISH_DIR/$APP_NAME.exe" >&2
  exit 1
fi

echo "==> Preparing AppDir"
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"

cp "$ROOT_DIR/packaging/linux/AppRun" "$APPDIR/AppRun"
cp "$ROOT_DIR/packaging/linux/NSMBe5.desktop" "$APPDIR/NSMBe5.desktop"
cp "$ROOT_DIR/packaging/linux/NSMBe5.desktop" "$APPDIR/usr/share/applications/NSMBe5.desktop"

cp -a "$PUBLISH_DIR/." "$APPDIR/usr/bin/"
cp "$ROOT_DIR/NSMBe5/nsmbe.ico" "$APPDIR/nsmbe.ico"
cp "$ROOT_DIR/NSMBe5/nsmbe.ico" "$APPDIR/usr/share/icons/hicolor/256x256/apps/nsmbe.ico"

# appimagetool expects a .png/.svg/.xpm icon matching the desktop Icon name.
if command -v magick >/dev/null 2>&1; then
  magick "$ROOT_DIR/NSMBe5/nsmbe.ico[0]" -resize 256x256 "$APPDIR/nsmbe.png"
elif command -v convert >/dev/null 2>&1; then
  convert "$ROOT_DIR/NSMBe5/nsmbe.ico[0]" -resize 256x256 "$APPDIR/nsmbe.png"
else
  # Fallback so packaging can continue even without ImageMagick.
  cp "$ROOT_DIR/NSMBe5/Resources/Icons/gfxeditor.png" "$APPDIR/nsmbe.png"
fi

cp "$APPDIR/nsmbe.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/nsmbe.png"

if [[ "$BUNDLE_MONO" -eq 1 ]]; then
  require_cmd mono
  echo "==> Bundling Mono runtime"
  bundle_mono_runtime
fi

# AppImage launcher metadata expects .DirIcon in AppDir root.
ln -sf "nsmbe.png" "$APPDIR/.DirIcon"

mkdir -p "$OUT_DIR"
if [[ "$BUNDLE_MONO" -eq 1 ]]; then
  OUTPUT_APPIMAGE="$OUT_DIR/NSMBe5-linux-x86_64-bundled-mono.AppImage"
else
  OUTPUT_APPIMAGE="$OUT_DIR/NSMBe5-linux-x86_64.AppImage"
fi

echo "==> Creating AppImage"
attempt=1
max_attempts=3
until ARCH=x86_64 appimagetool "$APPDIR" "$OUTPUT_APPIMAGE"; do
  if [[ "$attempt" -ge "$max_attempts" ]]; then
    echo "appimagetool failed after $max_attempts attempts." >&2
    exit 1
  fi
  attempt=$((attempt + 1))
  echo "Retrying appimagetool ($attempt/$max_attempts)..." >&2
  sleep 2
done
chmod +x "$OUTPUT_APPIMAGE"

echo "AppImage created: $OUTPUT_APPIMAGE"
if [[ "$BUNDLE_MONO" -eq 1 ]]; then
  echo "Mono runtime is bundled in this AppImage."
else
  echo "Note: This bundle uses system Mono unless you pass --bundle-mono."
fi
