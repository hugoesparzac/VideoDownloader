#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-0.1.0}"
RIDS="${RIDS:-linux-x64 win-x64 osx-arm64 osx-x64}"
PROJECT="src/VideoDownloader.App"
OUT="artifacts"

rm -rf "$OUT"
mkdir -p "$OUT"

for RID in $RIDS; do
  DIR="$OUT/publish/$RID"
  NAME="VideoDownloader-$VERSION-$RID"

  echo "==> Publishing $RID"
  dotnet publish "$PROJECT" -c Release -r "$RID" --self-contained \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:DebugType=None -p:DebugSymbols=false \
    -p:Version="$VERSION" \
    -o "$DIR"

  if [[ "$RID" == linux-* ]]; then
    cp packaging/linux/install.sh packaging/linux/uninstall.sh \
       packaging/linux/videodownloader.desktop.in "$DIR/"
    cp "$PROJECT/Assets/app-icon.png" "$DIR/videodownloader.png"
    chmod +x "$DIR/install.sh" "$DIR/uninstall.sh"
  fi

  if [[ "$RID" == win-* ]]; then
    (cd "$DIR" && zip -qr "../../$NAME.zip" .)
  else
    tar -czf "$OUT/$NAME.tar.gz" -C "$DIR" .
  fi
done

echo "Done. Packages are in $OUT/"