#!/usr/bin/env bash
set -euo pipefail

DATA="${XDG_DATA_HOME:-$HOME/.local/share}"

rm -rf "$HOME/.local/opt/VideoDownloader"
rm -f "$DATA/icons/hicolor/256x256/apps/videodownloader.png"
rm -f "$DATA/applications/videodownloader.desktop"
update-desktop-database "$DATA/applications" 2>/dev/null || true

echo "Video Downloader removed."
echo "Your settings (~/.config/VideoDownloader) and downloaded tools (~/.local/share/VideoDownloader) were kept."
