#!/usr/bin/env bash
set -euo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DATA="${XDG_DATA_HOME:-$HOME/.local/share}"
APP_DIR="$HOME/.local/opt/VideoDownloader"
ICON_DIR="$DATA/icons/hicolor/256x256/apps"
DESKTOP_DIR="$DATA/applications"

mkdir -p "$APP_DIR" "$ICON_DIR" "$DESKTOP_DIR"

install -m 755 "$SRC/VideoDownloader" "$APP_DIR/VideoDownloader"
install -m 644 "$SRC/videodownloader.png" "$ICON_DIR/videodownloader.png"

sed -e "s|@EXEC@|$APP_DIR/VideoDownloader|" \
    -e "s|@ICON@|$ICON_DIR/videodownloader.png|" \
    "$SRC/videodownloader.desktop.in" > "$DESKTOP_DIR/videodownloader.desktop"

update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true

echo "Video Downloader installed. Look for it in your applications menu."
