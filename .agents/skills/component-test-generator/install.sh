#!/usr/bin/env bash
# install.sh — Installs, updates, or uninstalls the component-test-generator agent skill.
#
# Usage:
#   ./install.sh              # install (symlink, default)
#   ./install.sh --copy       # install by copying files
#   ./install.sh update       # re-create symlink or re-copy
#   ./install.sh uninstall    # remove installed skill
#   ./install.sh --location ~/my/skills  # custom install directory

set -euo pipefail

SKILL_NAME="component-test-generator"
SKILL_SOURCE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"  # absolute path to this script's dir
INSTALL_DIR="${HOME}/.agents/skills"
ACTION="install"
USE_COPY=false

# ── Argument parsing ──────────────────────────────────────────────────────────

while [[ $# -gt 0 ]]; do
    case "$1" in
        install|update|uninstall)
            ACTION="$1"
            shift
            ;;
        --copy|-c)
            USE_COPY=true
            shift
            ;;
        --location|-l)
            INSTALL_DIR="$2"
            shift 2
            ;;
        *)
            echo "Unknown argument: $1"
            echo "Usage: ./install.sh [install|update|uninstall] [--copy] [--location <dir>]"
            exit 1
            ;;
    esac
done

TARGET_PATH="${INSTALL_DIR}/${SKILL_NAME}"

# ── Helpers ───────────────────────────────────────────────────────────────────

cyan='\033[0;36m'
green='\033[0;32m'
yellow='\033[1;33m'
reset='\033[0m'

step()  { echo -e "  ${cyan}$*${reset}"; }
ok()    { echo -e "  ${green}✓ $*${reset}"; }
warn()  { echo -e "  ${yellow}! $*${reset}"; }

is_symlink() { [[ -L "$1" ]]; }

remove_target() {
    local path="$1"
    if is_symlink "$path"; then
        rm "$path"
        ok "Removed symlink: $path"
    elif [[ -d "$path" ]]; then
        rm -rf "$path"
        ok "Removed copy: $path"
    else
        warn "Nothing to remove at: $path"
    fi
}

install_skill() {
    local target="$1"
    local parent
    parent="$(dirname "$target")"

    mkdir -p "$parent"

    if [[ "$USE_COPY" == true ]]; then
        cp -r "$SKILL_SOURCE" "$target"
        ok "Copied skill to: $target"
    else
        ln -s "$SKILL_SOURCE" "$target"
        ok "Symlinked: $target  ->  $SKILL_SOURCE"
    fi
}

# ── Actions ───────────────────────────────────────────────────────────────────

case "$ACTION" in

    install)
        echo ""
        echo "Installing ${SKILL_NAME} skill..."

        if [[ -e "$TARGET_PATH" || -L "$TARGET_PATH" ]]; then
            if is_symlink "$TARGET_PATH"; then
                existing="$(readlink "$TARGET_PATH")"
                if [[ "$existing" == "$SKILL_SOURCE" ]]; then
                    ok "Already installed (symlink is correct): $TARGET_PATH"
                    exit 0
                else
                    warn "Symlink exists but points elsewhere: $existing"
                    warn "Run './install.sh update' to fix it."
                    exit 1
                fi
            else
                warn "A non-symlink directory already exists at: $TARGET_PATH"
                warn "Run './install.sh update' to replace it."
                exit 1
            fi
        fi

        install_skill "$TARGET_PATH"

        echo ""
        echo "  Skill installed. Restart your agent to pick it up."
        echo "  Run '/skills' in Copilot Chat (agent mode) to verify."
        echo ""
        ;;

    update)
        echo ""
        echo "Updating ${SKILL_NAME} skill..."

        if [[ -e "$TARGET_PATH" || -L "$TARGET_PATH" ]]; then
            remove_target "$TARGET_PATH"
        else
            warn "No existing install found at $TARGET_PATH — installing fresh."
        fi

        install_skill "$TARGET_PATH"

        echo ""
        echo "  Skill updated. Restart your agent to pick up any changes."
        echo ""
        ;;

    uninstall)
        echo ""
        echo "Uninstalling ${SKILL_NAME} skill..."

        remove_target "$TARGET_PATH"

        echo ""
        echo "  Uninstall complete."
        echo ""
        ;;

esac
