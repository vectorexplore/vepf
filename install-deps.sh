#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Default to the bundled libmilcorez.so if no explicit path is passed
SO_PATH="${1:-}"
if [[ -z "$SO_PATH" ]]; then
    shopt -s nullglob
    matches=("$SCRIPT_DIR"/shared/Microsoft.WindowsDesktop.App/*/libmilcorez.so)
    shopt -u nullglob
    SO_PATH="${matches[0]:-}"
fi

SUDO=""
[[ $EUID -ne 0 ]] && SUDO="sudo"

# Ubuntu
if command -v apt-get >/dev/null 2>&1; then
    pkgs=(
        libvips42 libvulkan1 mesa-vulkan-drivers
        libxkbcommon0 libx11-6 libwayland-client0
        libgstreamer1.0-0 libgstreamer-plugins-base1.0-0
    )
    missing=()
    for p in "${pkgs[@]}"; do
        if ! { dpkg-query -W -f='${Status}\n' "$p" "${p}t64" 2>/dev/null || true; } | grep -q "install ok installed"; then
            missing+=("$p")
        fi
    done

    if [[ ${#missing[@]} -eq 0 ]]; then
        echo "All required system packages are already installed."
    else
        $SUDO apt-get update -qq
        $SUDO env DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends "${missing[@]}"
    fi

# Rocky Linux 9 / RHEL 9 / Fedora
elif command -v dnf >/dev/null 2>&1; then
    pkgs=(
        vips vips-magick vulkan-loader mesa-vulkan-drivers
        libxkbcommon libX11 libwayland-client
        gstreamer1 gstreamer1-plugins-base
    )
    missing=()
    for p in "${pkgs[@]}"; do
        rpm -q "$p" >/dev/null 2>&1 || missing+=("$p")
    done

    if [[ ${#missing[@]} -eq 0 ]]; then
        echo "All required system packages are already installed."
    else
        vips_missing=()
        for p in "${missing[@]}"; do
            if [[ "$p" == "vips" || "$p" == "vips-magick" ]]; then
                vips_missing+=("$p")
            fi
        done

        if [[ ${#vips_missing[@]} -gt 0 ]] && ! dnf info "${vips_missing[@]}" >/dev/null 2>&1; then
            el_ver="$(rpm -E %rhel 2>/dev/null || true)"
            if [[ "$el_ver" =~ ^[0-9]+$ ]] && (( el_ver >= 9 )); then
                # Enable CRB (required for EPEL/Remi vips dependencies on EL9/EL10+)
                $SUDO /usr/bin/crb enable 2>/dev/null \
                    || $SUDO dnf config-manager --set-enabled crb 2>/dev/null \
                    || $SUDO dnf config-manager setopt crb.enabled=1 2>/dev/null \
                    || $SUDO subscription-manager repos --enable "codeready-builder-for-rhel-${el_ver}-$(uname -m)-rpms" 2>/dev/null \
                    || true

                # Ensure EPEL is installed (dependency base for Remi)
                if ! rpm -q epel-release >/dev/null 2>&1; then
                    if dnf info epel-release >/dev/null 2>&1; then
                        $SUDO dnf install -y epel-release
                    else
                        $SUDO dnf install -y "https://dl.fedoraproject.org/pub/epel/epel-release-latest-${el_ver}.noarch.rpm"
                    fi
                fi

                # Install missing vips / vips-magick packages strictly from Remi
                if ! dnf info "${vips_missing[@]}" >/dev/null 2>&1; then
                    if ! rpm -q remi-release >/dev/null 2>&1; then
                        $SUDO dnf install -y "https://rpms.remirepo.net/enterprise/remi-release-${el_ver}.rpm"
                    fi
                    $SUDO dnf install -y --setopt=install_weak_deps=False --enablerepo=remi "${vips_missing[@]}"

                    filtered=()
                    for p in "${missing[@]}"; do
                        if [[ "$p" != "vips" && "$p" != "vips-magick" ]]; then
                            filtered+=("$p")
                        fi
                    done
                    missing=("${filtered[@]}")
                fi
            fi
        fi

        if [[ ${#missing[@]} -gt 0 ]]; then
            $SUDO dnf install -y --setopt=install_weak_deps=False "${missing[@]}"
        fi
    fi

# Arch Linux
elif command -v pacman >/dev/null 2>&1; then
    pkgs=(
        libvips imagemagick vulkan-icd-loader vulkan-swrast vulkan-intel vulkan-radeon
        libxkbcommon libx11 wayland
        gstreamer gst-plugins-base-libs
        # noto-fonts noto-fonts-extra noto-fonts-cjk noto-fonts-emoji
    )
    missing=()
    for p in "${pkgs[@]}"; do
        pacman -Q "$p" >/dev/null 2>&1 || missing+=("$p")
    done

    if [[ ${#missing[@]} -eq 0 ]]; then
        echo "All required system packages are already installed."
    else
        $SUDO pacman -Syu --needed --noconfirm "${missing[@]}"
    fi

elif command -v zypper >/dev/null 2>&1; then
    pkgs=(
        libvips42 libvulkan1 libvulkan_lvp libvulkan_intel libvulkan_radeon
        libxkbcommon0 libX11-6 libwayland-client0
        gstreamer gstreamer-plugins-base
    )
    missing=()
    for p in "${pkgs[@]}"; do
        rpm -q "$p" >/dev/null 2>&1 || missing+=("$p")
    done

    if [[ ${#missing[@]} -eq 0 ]]; then
        echo "All required system packages are already installed."
    else
        if printf '%s\n' "${missing[@]}" | grep -qx "libvips42"; then
            if ! zypper se -x libvips42 >/dev/null 2>&1; then
                leap_ver="$(. /etc/os-release && echo "${VERSION_ID:-16.0}")"
                $SUDO zypper ar -f "https://download.opensuse.org/repositories/graphics/${leap_ver}/" graphics 2>/dev/null || true
                $SUDO zypper --gpg-auto-import-keys refresh
            fi
        fi
        $SUDO zypper --non-interactive --gpg-auto-import-keys install --no-recommends "${missing[@]}"
    fi

else
    echo "Error: Unsupported package manager (expected apt-get, dnf, pacman, or zypper)." >&2
    exit 1
fi

if [[ -n "$SO_PATH" && -f "$SO_PATH" ]]; then
    MISSING=$(ldd "$SO_PATH" | grep "not found" || true)
    if [[ -n "$MISSING" ]]; then
        echo "Error: Unresolved shared libraries in $SO_PATH:" >&2
        echo "$MISSING" >&2
        exit 1
    fi
    echo "Verified: all native dependencies for $SO_PATH are resolved."
fi
