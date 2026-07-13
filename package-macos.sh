#!/usr/bin/env bash
# Builds a double-clickable macOS .app bundle for the OCPP 1.6 simulator.
# Run this ON a Mac that has the .NET 8 SDK installed.
#
#   chmod +x package-macos.sh
#   ./package-macos.sh            # builds for the Mac's own architecture
#   ./package-macos.sh osx-x64    # force Intel
#   ./package-macos.sh osx-arm64  # force Apple Silicon
#
# Output: publish/OcppSimulator.app  (drag into /Applications)

set -euo pipefail
cd "$(dirname "$0")"

PROJ="OcppSimulator.Mac/OcppSimulator.Mac.csproj"
APP_NAME="OcppSimulator"
EXE="OcppSimulator.Mac"
ICON_PNG="OcppSimulator.Mac/Assets/appicon.png"

# Pick runtime identifier
if [[ $# -ge 1 ]]; then
  RID="$1"
elif [[ "$(uname -m)" == "arm64" ]]; then
  RID="osx-arm64"
else
  RID="osx-x64"
fi
echo "Building for $RID ..."

PUBLISH_DIR="publish/$RID"
APP="publish/${APP_NAME}.app"

# 1) Publish self-contained
dotnet publish "$PROJ" -c Release -r "$RID" --self-contained true -o "$PUBLISH_DIR"

# 2) Assemble the .app bundle
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "OcppSimulator.Mac/Info.plist" "$APP/Contents/Info.plist"
cp -R "$PUBLISH_DIR/." "$APP/Contents/MacOS/"
chmod +x "$APP/Contents/MacOS/$EXE"

# 3) Generate the .icns app icon from the PNG (uses macOS built-in tools)
if command -v iconutil >/dev/null 2>&1; then
  ICONSET="$(mktemp -d)/appicon.iconset"
  mkdir -p "$ICONSET"
  for s in 16 32 64 128 256 512; do
    sips -z $s $s     "$ICON_PNG" --out "$ICONSET/icon_${s}x${s}.png"      >/dev/null
    sips -z $((s*2)) $((s*2)) "$ICON_PNG" --out "$ICONSET/icon_${s}x${s}@2x.png" >/dev/null
  done
  iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/appicon.icns"
  echo "Icon embedded."
else
  echo "iconutil not found; skipping .icns (app still runs, uses window icon at runtime)."
fi

# 4) Ad-hoc sign. Apple Silicon refuses to launch any unsigned binary, and a
#    bundle assembled by copying files has no signature of its own.
codesign --force --deep --sign - "$APP"

# 5) Clear the quarantine flag so Gatekeeper doesn't block the unsigned app
xattr -dr com.apple.quarantine "$APP" 2>/dev/null || true

# 6) Build a compressed .dmg with a drag-to-Applications shortcut
DMG="publish/${APP_NAME}-1.0.0-${RID}.dmg"
STAGING="$(mktemp -d)/dmg"
mkdir -p "$STAGING"
cp -R "$APP" "$STAGING/"
ln -s /Applications "$STAGING/Applications"
rm -f "$DMG"
hdiutil create -volname "$APP_NAME" -srcfolder "$STAGING" -ov -format UDZO "$DMG" >/dev/null
xattr -dr com.apple.quarantine "$DMG" 2>/dev/null || true

echo ""
echo "Done:"
echo "  App: $APP"
echo "  DMG: $DMG"
echo ""
echo "DMG'yi açıp uygulamayı Applications'a sürükleyin."
echo "İmzasız olduğu için ilk açılışta uyarı çıkarsa: sağ tık > Aç,"
echo "ya da: xattr -dr com.apple.quarantine /Applications/${APP_NAME}.app"
