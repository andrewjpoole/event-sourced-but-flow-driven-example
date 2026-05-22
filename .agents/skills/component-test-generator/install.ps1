#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs, updates, or uninstalls the component-test-generator agent skill.

.DESCRIPTION
    Manages the skill in the standard cross-client user-level location
    (~/.agents/skills/) and optionally in client-specific locations.

    By default, a symbolic link is created so edits to the skill files in this
    repo are immediately reflected everywhere. Use -Copy to copy the files
    instead (safer if you plan to move the repo).

.PARAMETER Action
    install   Create the symlink/copy (default).
    update    Refresh the symlink or re-copy the files.
    uninstall Remove all installed locations.

.PARAMETER Copy
    Copy files instead of creating a symbolic link.

.PARAMETER Location
    Override the install directory. Defaults to ~/.agents/skills/.

.EXAMPLE
    # Install using a symlink (default):
    .\install.ps1

    # Install by copying files:
    .\install.ps1 -Copy

    # Uninstall:
    .\install.ps1 uninstall

    # Update (re-create symlink or re-copy after moving the repo):
    .\install.ps1 update
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Position = 0)]
    [ValidateSet('install', 'update', 'uninstall')]
    [string]$Action = 'install',

    [switch]$Copy,

    [string]$Location = (Join-Path $HOME '.agents' 'skills')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$SkillName   = 'component-test-generator'
$SkillSource = $PSScriptRoot          # The script lives at the skill root
$TargetPath  = Join-Path $Location $SkillName

# ── Helpers ──────────────────────────────────────────────────────────────────

function Write-Step([string]$msg) { Write-Host "  $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host "  ✓ $msg" -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "  ! $msg" -ForegroundColor Yellow }
function Write-Fail([string]$msg) { Write-Host "  ✗ $msg" -ForegroundColor Red }

function Test-IsSymlink([string]$path) {
    $item = Get-Item -Path $path -Force -ErrorAction SilentlyContinue
    return $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)
}

function Remove-SkillTarget([string]$path) {
    if (Test-IsSymlink $path) {
        (Get-Item -Path $path -Force).Delete()
        Write-Ok "Removed symlink: $path"
    } elseif (Test-Path $path) {
        Remove-Item -Recurse -Force $path
        Write-Ok "Removed copy: $path"
    } else {
        Write-Warn "Nothing to remove at: $path"
    }
}

function Install-Skill([string]$target, [bool]$useCopy) {
    $parentDir = Split-Path $target -Parent
    if (-not (Test-Path $parentDir)) {
        New-Item -ItemType Directory -Force -Path $parentDir | Out-Null
        Write-Step "Created directory: $parentDir"
    }

    if ($useCopy) {
        Copy-Item -Recurse -Force $SkillSource $target
        Write-Ok "Copied skill to: $target"
    } else {
        # Symlink requires either Admin rights or Developer Mode on Windows.
        try {
            New-Item -ItemType SymbolicLink -Path $target -Target $SkillSource | Out-Null
            Write-Ok "Symlinked: $target  ->  $SkillSource"
        } catch [UnauthorizedAccessException] {
            Write-Warn "Cannot create symlink without admin rights or Developer Mode."
            Write-Warn "Falling back to file copy. Re-run with -Copy to suppress this message."
            Copy-Item -Recurse -Force $SkillSource $target
            Write-Ok "Copied skill to: $target"
        }
    }
}

# ── Actions ───────────────────────────────────────────────────────────────────

switch ($Action) {

    'install' {
        Write-Host "`nInstalling $SkillName skill..." -ForegroundColor White

        if (Test-Path $TargetPath) {
            if (Test-IsSymlink $TargetPath) {
                $existing = (Get-Item -Path $TargetPath -Force).Target
                if ($existing -eq $SkillSource) {
                    Write-Ok "Already installed (symlink is correct): $TargetPath"
                    exit 0
                } else {
                    Write-Warn "Symlink exists but points elsewhere: $existing"
                    Write-Warn "Run '.\install.ps1 update' to fix it."
                    exit 1
                }
            } else {
                Write-Warn "A non-symlink directory already exists at: $TargetPath"
                Write-Warn "Run '.\install.ps1 update' to replace it."
                exit 1
            }
        }

        Install-Skill $TargetPath $Copy.IsPresent

        Write-Host ""
        Write-Host "  Skill installed. Restart your agent to pick it up." -ForegroundColor White
        Write-Host "  Run '/skills' in Copilot Chat (agent mode) to verify." -ForegroundColor Gray
        Write-Host ""
    }

    'update' {
        Write-Host "`nUpdating $SkillName skill..." -ForegroundColor White

        if (Test-Path $TargetPath) {
            Remove-SkillTarget $TargetPath
        } else {
            Write-Warn "No existing install found at $TargetPath — installing fresh."
        }

        Install-Skill $TargetPath $Copy.IsPresent

        Write-Host ""
        Write-Host "  Skill updated. Restart your agent to pick up any changes." -ForegroundColor White
        Write-Host ""
    }

    'uninstall' {
        Write-Host "`nUninstalling $SkillName skill..." -ForegroundColor White

        Remove-SkillTarget $TargetPath

        Write-Host ""
        Write-Host "  Uninstall complete." -ForegroundColor White
        Write-Host ""
    }
}
