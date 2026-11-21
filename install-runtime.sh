#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPT_PATH="$SCRIPT_DIR/$(basename "${BASH_SOURCE[0]}")"
ORIG_ARGS=("$@")
SPECIFIED_VERSION=""

while getopts "v:h" opt; do
    case "$opt" in
        v) SPECIFIED_VERSION="$OPTARG" ;;
        h) echo "Usage: $0 [-v <dotnet-version>]"; exit 0 ;;
        \?) echo "Usage: $0 [-v <dotnet-version>]" >&2; exit 1 ;;
    esac
done

# Resolve invoking user's home in case the script is run via sudo
USER_HOME="$HOME"
if [[ -n "${SUDO_USER:-}" ]]; then
    USER_HOME=$(getent passwd "$SUDO_USER" | cut -d: -f6)
fi

DOTNET_BIN=""
for candidate in \
    "$(command -v dotnet 2>/dev/null || true)" \
    "${DOTNET_ROOT:+$DOTNET_ROOT/dotnet}" \
    "$USER_HOME/dotnet/dotnet" \
    "$USER_HOME/.dotnet/dotnet" \
    "/usr/share/dotnet/dotnet" \
    "/usr/lib/dotnet/dotnet" \
    "/usr/lib64/dotnet/dotnet"; do
    if [[ -n "$candidate" && -x "$candidate" ]]; then
        DOTNET_BIN="$candidate"
        break
    fi
done

if [[ -z "$DOTNET_BIN" ]]; then
    echo "Error: 'dotnet' not found in PATH, \$DOTNET_ROOT, \$HOME/dotnet, or system dotnet paths." >&2
    exit 1
fi

# =========================================================
# 1. Detect .NET 10.0 SDK (Preferred) or Runtime-Only Host
# =========================================================
HAS_SDK=false
SDK_LINE=""
RUNTIME_LINE=""

if [[ -n "$SPECIFIED_VERSION" ]]; then
    SDK_LINE=$("$DOTNET_BIN" --list-sdks 2>/dev/null | grep -F "${SPECIFIED_VERSION} [" | tail -n 1 || true)
    if [[ -z "$SDK_LINE" ]]; then
        RUNTIME_LINE=$("$DOTNET_BIN" --list-runtimes 2>/dev/null | grep -F "Microsoft.NETCore.App ${SPECIFIED_VERSION} [" | tail -n 1 || true)
    fi
else
    SDK_LINE=$("$DOTNET_BIN" --list-sdks 2>/dev/null | grep "^10\.0\." | sort -V | tail -n 1 || true)
    if [[ -z "$SDK_LINE" ]]; then
        RUNTIME_LINE=$("$DOTNET_BIN" --list-runtimes 2>/dev/null | grep "^Microsoft\.NETCore\.App 10\.0\." | sort -V | tail -n 1 || true)
    fi
fi

if [[ -n "$SDK_LINE" ]]; then
    HAS_SDK=true
    TARGET_VERSION=$(printf '%s\n' "$SDK_LINE" | awk '{print $1}')
    SDK_DIR=$(printf '%s\n' "$SDK_LINE" | sed -n 's/.*\[\(.*\)\].*/\1/p')
    DOTNET_ROOT=$(dirname "$SDK_DIR")
elif [[ -n "$RUNTIME_LINE" ]]; then
    HAS_SDK=false
    TARGET_VERSION=$(printf '%s\n' "$RUNTIME_LINE" | awk '{print $2}')
    RUNTIME_DIR=$(printf '%s\n' "$RUNTIME_LINE" | sed -n 's/.*\[\(.*\)\].*/\1/p')
    DOTNET_ROOT=$(dirname "$(dirname "$RUNTIME_DIR")")
else
    echo "Error: Matching .NET 10.0 SDK or Microsoft.NETCore.App runtime not found via $DOTNET_BIN." >&2
    exit 1
fi

if [[ "$(printf '%s\n' "10.0.0" "$TARGET_VERSION" | sort -V | head -n1)" != "10.0.0" ]] || \
   [[ "$(printf '%s\n' "10.0.999" "$TARGET_VERSION" | sort -V | tail -n1)" != "10.0.999" ]]; then
    echo "Error: .NET version $TARGET_VERSION is outside supported range (10.0.0 - 10.0.999)." >&2
    exit 1
fi

# =========================================================
# 2. Pre-Flight Write Permission Check (Before Modifying Any Files)
# =========================================================
check_paths=(
    "$DOTNET_ROOT"
    "$DOTNET_ROOT/shared"
    "$DOTNET_ROOT/shared/Microsoft.WindowsDesktop.App"
)
if [[ "$HAS_SDK" == true ]]; then
    check_paths+=(
        "$SDK_DIR/$TARGET_VERSION"
        "$DOTNET_ROOT/packs"
        "$DOTNET_ROOT/packs/Microsoft.WindowsDesktop.App.Ref"
    )
fi

needs_root=false
for check_path in "${check_paths[@]}"; do
    if [[ -e "$check_path" && ! -w "$check_path" ]]; then
        needs_root=true
        break
    fi
done

if [[ "$needs_root" == true ]]; then
    if [[ $EUID -ne 0 ]]; then
        if command -v sudo >/dev/null 2>&1; then
            echo "System .NET path detected at $DOTNET_ROOT (requires root privileges). Re-running with sudo..."
            exec sudo env "DOTNET_ROOT=$DOTNET_ROOT" "PATH=$PATH" bash "$SCRIPT_PATH" "${ORIG_ARGS[@]}"
        else
            echo "Error: No write permission to $DOTNET_ROOT and 'sudo' is not available. Re-run as root." >&2
            exit 1
        fi
    else
        echo "Error: $DOTNET_ROOT is not writable even as root (filesystem may be read-only)." >&2
        exit 1
    fi
fi

# =========================================================
# 3. Install SDK Components (If Present) & Shared Desktop Runtime
# =========================================================
if [[ "$HAS_SDK" == true ]]; then
    echo "Installing to .NET SDK $TARGET_VERSION ($DOTNET_ROOT)..."
    [[ -x "$SCRIPT_DIR/tools/UpdateSdkProps" ]] || chmod +x "$SCRIPT_DIR/tools/UpdateSdkProps"
    (cd "$SCRIPT_DIR/tools" && ./UpdateSdkProps "$SDK_DIR/$TARGET_VERSION")

    mkdir -p "$DOTNET_ROOT/packs/Microsoft.WindowsDesktop.App.Ref"
    cp -a "$SCRIPT_DIR/packs/Microsoft.WindowsDesktop.App.Ref/." "$DOTNET_ROOT/packs/Microsoft.WindowsDesktop.App.Ref/"
else
    echo "Installing to .NET Runtime $TARGET_VERSION ($DOTNET_ROOT, runtime-only mode)..."
fi

VERSION=$(ls -1 "$SCRIPT_DIR/shared/Microsoft.WindowsDesktop.App" | sort -V | tail -n 1)
DEST_SHARED_VER="$DOTNET_ROOT/shared/Microsoft.WindowsDesktop.App/$VERSION"

mkdir -p "$DEST_SHARED_VER"
cp -a "$SCRIPT_DIR/shared/Microsoft.WindowsDesktop.App/$VERSION/." "$DEST_SHARED_VER/"

(
    cd "$DEST_SHARED_VER"
    for name in gdi32 gdiplus imm32 kernel32 user32; do
        ln -snf libmilcorez.so "${name}.dll.so"
    done
)

MISSING=$(ldd "$DEST_SHARED_VER/libmilcorez.so" | grep "not found" || true)
if [[ -n "$MISSING" ]]; then
    echo "Warning: .NET files installed, but system libraries are missing:" >&2
    echo "$MISSING" >&2
    echo "Run 'sudo bash \"$SCRIPT_DIR/install-deps.sh\"' to install missing OS packages." >&2
fi

echo "VePF Desktop Runtime $VERSION installed cleanly to $DOTNET_ROOT."
