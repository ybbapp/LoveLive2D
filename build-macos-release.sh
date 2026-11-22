#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-1.0.0}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT="$ROOT/bin/Release"
mkdir -p "$OUTPUT"

for arch in arm64 x64; do
    publish="$ROOT/bin/Release/net8.0/osx-$arch/publish"
    asset="$OUTPUT/LoveLive2D-v$VERSION-net8.0-osx-$arch-self-contained.zip"

    dotnet publish "$ROOT/LoveLive2D.csproj" \
        -c Release \
        -f net8.0 \
        -r "osx-$arch" \
        --self-contained true

    mkdir -p "$publish/lib"
    cp -f "$ROOT/mod/rlm1501/rlm1501.jar" "$publish/lib/"
    cp -f "$ROOT/mod/rlm1603/rlm1603.jar" "$publish/lib/"

    rm -f "$asset"
    (
        cd "$publish"
        zip -qry "$asset" .
    )

    unzip -tq "$asset" >/dev/null
    entries="$(unzip -Z1 "$asset")"
    for required in LoveLive2D libhostfxr.dylib lib/rlm1501.jar lib/rlm1603.jar; do
        grep -Fqx "$required" <<<"$entries" \
            || { echo "Missing $required in $asset" >&2; exit 1; }
    done
    shasum -a 256 "$asset"
done
