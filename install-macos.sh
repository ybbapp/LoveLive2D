#!/usr/bin/env bash
set -euo pipefail

REPOSITORY="ybbapp/LoveLive2D"
RELEASE_VERSION="${LOVELIVE2D_VERSION:-v1.0.0}"
RELEASE_URL="${LOVELIVE2D_RELEASE_URL:-}"
INSTALL_ROOT="${LOVELIVE2D_HOME:-$HOME/Applications/LoveLive2D}"
INSTALL_DIR="$INSTALL_ROOT/app"
COMMAND_PATH="$INSTALL_ROOT/LoveLive2D.command"
case "$(uname -m)" in
    arm64) ARCH="arm64" ;;
    x86_64) ARCH="x64" ;;
    *) ARCH="unsupported" ;;
esac
ASSET_NAME="LoveLive2D-v1.0.0-net8.0-osx-$ARCH-self-contained.zip"
WORK_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/lovelive2d-install.XXXXXX")"
SOURCE_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cleanup() {
    rm -r "$WORK_ROOT"
}
trap cleanup EXIT

say() {
    printf '%s\n' "$*"
}

fail() {
    printf '错误：%s\n' "$*" >&2
    exit 1
}

require_command() {
    command -v "$1" >/dev/null 2>&1 || fail "缺少命令：$1"
}

has_bundled_rlm_jars() {
    local directory="$1"
    [[ -f "$directory/lib/rlm1501.jar" && -f "$directory/lib/rlm1603.jar" ]]
}

download_install_package() {
    local url
    if [[ -n "$RELEASE_URL" ]]; then
        url="$RELEASE_URL"
    else
        url="https://github.com/$REPOSITORY/releases/download/$RELEASE_VERSION/$ASSET_NAME"
    fi

    say "下载 $ARCH 自包含安装包..."
    if ! curl -fL --retry 3 --connect-timeout 15 -o "$WORK_ROOT/package.zip" "$url"; then
        return 1
    fi
    unzip -q "$WORK_ROOT/package.zip" -d "$WORK_ROOT/package"
}

build_install_package() {
    [[ -f "$SOURCE_ROOT/LoveLive2D.csproj" ]] \
        || fail "没有找到可下载的发布包，请设置 LOVELIVE2D_VERSION 或从源码目录运行安装脚本。"

    require_command dotnet
    say "没有找到发布包，正在从源码构建..."
    dotnet publish "$SOURCE_ROOT/LoveLive2D.csproj" \
        -c Release \
        -f net8.0 \
        -r "osx-$ARCH" \
        --self-contained true

    local publish_dir="$SOURCE_ROOT/bin/Release/net8.0/osx-$ARCH/publish"
    mkdir -p "$publish_dir/lib"
    cp -f "$SOURCE_ROOT/mod/rlm1501/rlm1501.jar" "$publish_dir/lib/"
    cp -f "$SOURCE_ROOT/mod/rlm1603/rlm1603.jar" "$publish_dir/lib/"
}

prepare_local_install() {
    local source="$1"
    rm -rf "$INSTALL_DIR"
    mkdir -p "$INSTALL_ROOT"
    cp -R "$source" "$INSTALL_DIR"
    has_bundled_rlm_jars "$INSTALL_DIR" \
        || fail "安装包缺少 Live2D 5.0 或 5.3 的 RLM 补丁 JAR。"
    [[ -f "$INSTALL_DIR/libhostfxr.dylib" ]] \
        || fail "安装包缺少自包含 .NET 运行时。"
    chmod +x "$INSTALL_DIR/LoveLive2D"
}

install_launcher() {
    cat >"$COMMAND_PATH" <<EOF
#!/bin/bash
exec "$INSTALL_DIR/LoveLive2D" "\$@"
EOF
    chmod +x "$COMMAND_PATH"
}

[[ "$(uname -s)" == "Darwin" ]] || fail "此脚本仅支持 macOS。"
case "$(uname -m)" in
    arm64) ARCH="arm64" ;;
    x86_64) ARCH="x64" ;;
    *) fail "不支持的 CPU 架构：$(uname -m)" ;;
esac

require_command curl
require_command unzip

say "Love Live2D macOS 安装程序"
say "架构：$ARCH"
say "安装目录：$INSTALL_ROOT"

if [[ -x "$SOURCE_ROOT/bin/Release/net8.0/osx-$ARCH/publish/LoveLive2D" ]] \
    && has_bundled_rlm_jars "$SOURCE_ROOT/bin/Release/net8.0/osx-$ARCH/publish"
then
    say "使用当前源码目录中的发布文件。"
    prepare_local_install "$SOURCE_ROOT/bin/Release/net8.0/osx-$ARCH/publish"
elif [[ ! -f "$SOURCE_ROOT/LoveLive2D.csproj" ]] && download_install_package; then
    prepare_local_install "$WORK_ROOT/package"
elif download_install_package; then
    prepare_local_install "$WORK_ROOT/package"
else
    say "发布包下载失败。"
    build_install_package
    prepare_local_install "$SOURCE_ROOT/bin/Release/net8.0/osx-$ARCH/publish"
fi

install_launcher
say ""
say "安装完成。以后可在 Finder 中双击："
say "$COMMAND_PATH"
say ""
say "如果检测到 Live2D，可执行："
say "$COMMAND_PATH --patch --target '/Applications/Live2D Cubism 5.0/Live2D Cubism Editor 5.0.app'"
say ""
say "安装目录：$INSTALL_DIR"
