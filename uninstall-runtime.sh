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
# 1. Detect .NET 10.0 SDK or Runtime-Only Host & DOTNET_ROOT
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
    echo "Found Target .NET SDK: $TARGET_VERSION"
elif [[ -n "$RUNTIME_LINE" ]]; then
    HAS_SDK=false
    TARGET_VERSION=$(printf '%s\n' "$RUNTIME_LINE" | awk '{print $2}')
    RUNTIME_DIR=$(printf '%s\n' "$RUNTIME_LINE" | sed -n 's/.*\[\(.*\)\].*/\1/p')
    DOTNET_ROOT=$(dirname "$(dirname "$RUNTIME_DIR")")
    echo "Found Target .NET Runtime: $TARGET_VERSION (runtime-only mode)"
else
    echo "Error: No matching .NET 10.0 SDK or Microsoft.NETCore.App runtime found via $DOTNET_BIN." >&2
    exit 1
fi

echo "DOTNET_ROOT: $DOTNET_ROOT"

# =========================================================
# 2. Pre-Flight Write Permission Check
# =========================================================
DEST_PACK_DIR="$DOTNET_ROOT/packs/Microsoft.WindowsDesktop.App.Ref"
DEST_SHARED_DIR="$DOTNET_ROOT/shared/Microsoft.WindowsDesktop.App"

needs_root=false
for check_path in "$DOTNET_ROOT" "$DEST_PACK_DIR" "$DEST_SHARED_DIR"; do
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
# 3. Remove Installed Targeting Pack (SDK) & Shared Runtime
# =========================================================
VERSION=""
if [[ -d "$SCRIPT_DIR/shared/Microsoft.WindowsDesktop.App" ]]; then
    VERSION=$(ls -1 "$SCRIPT_DIR/shared/Microsoft.WindowsDesktop.App" | sort -V | tail -n 1 || true)
elif [[ -d "$DEST_SHARED_DIR" ]]; then
    VERSION=$(ls -1 "$DEST_SHARED_DIR" | sort -V | tail -n 1 || true)
fi
VERSION="${VERSION:-$TARGET_VERSION}"

removed_any=false

if [[ -e "$DEST_PACK_DIR" ]]; then
    echo "Deleting: $DEST_PACK_DIR"
    rm -rf "$DEST_PACK_DIR"
    removed_any=true
elif [[ "$HAS_SDK" == true ]]; then
    echo "Skipping: $DEST_PACK_DIR (not present)"
fi

if [[ -e "$DEST_SHARED_DIR" ]]; then
    echo "Deleting: $DEST_SHARED_DIR"
    rm -rf "$DEST_SHARED_DIR"
    removed_any=true
else
    echo "Skipping: $DEST_SHARED_DIR (not present)"
fi

if [[ "$removed_any" == true ]]; then
    echo "VePF Desktop Runtime $VERSION uninstalled cleanly from $DOTNET_ROOT."
else
    echo "VePF Desktop Runtime was not installed under $DOTNET_ROOT."
fi