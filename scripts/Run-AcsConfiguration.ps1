# ─────────────────────────────────────────────────────────────────
# Run ACS Custom Domain Configuration
# This script loads parameters and executes the configuration
# ─────────────────────────────────────────────────────────────────

$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Load parameters
Write-Host "Loading configuration parameters..." -ForegroundColor Cyan
$params = & "$ScriptDir\acs-config-params.ps1"

Write-Host ""
Write-Host "Configuration Parameters:" -ForegroundColor Yellow
Write-Host "  Resource Group:  $($params.ResourceGroup)" -ForegroundColor White
Write-Host "  ACS Resource:    $($params.AcsResourceName)" -ForegroundColor White
Write-Host "  Custom Domain:   $($params.CustomDomain)" -ForegroundColor White
Write-Host ""

# Confirm execution
$confirm = Read-Host "Proceed with ACS custom domain configuration? (y/n)"
if ($confirm -ne 'y') {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

# Execute configuration script
Write-Host ""
Write-Host "Starting ACS configuration..." -ForegroundColor Cyan
Write-Host ""

& "$ScriptDir\Configure-AcsCustomDomain.ps1" @params
