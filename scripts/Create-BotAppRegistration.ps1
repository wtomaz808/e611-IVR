# ─────────────────────────────────────────────────────────────────
# Create-BotAppRegistration.ps1
#
# Creates an Entra ID App Registration for the IVR Teams Calling Bot
# and assigns the Microsoft Graph Application permissions required
# for Teams calling bots.
#
# Run this BEFORE deploying infra/main.bicep. Copy the output App ID
# and Secret into infra/parameters/azuregov.bicepparam.
#
# Usage (Azure Government / GCC High):
#   az cloud set --name AzureUSGovernment
#   az login
#   .\Create-BotAppRegistration.ps1 -Environment AzureUSGovernment
#
# Usage (Commercial Azure):
#   az cloud set --name AzureCloud
#   az login
#   .\Create-BotAppRegistration.ps1 -Environment AzureCloud
# ─────────────────────────────────────────────────────────────────

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('AzureCloud', 'AzureUSGovernment')]
    [string]$Environment = 'AzureUSGovernment',

    [Parameter(Mandatory = $false)]
    [string]$AppName = 'IVR Teams Calling Bot',

    [Parameter(Mandatory = $false)]
    [string]$SecretDisplayName = 'IVR-Bot-Secret',

    # How many years until the secret expires (1–2 recommended)
    [Parameter(Mandatory = $false)]
    [int]$SecretYears = 1
)

$ErrorActionPreference = 'Stop'

# ─── Confirm cloud ───────────────────────────────────────────────
Write-Host ""
Write-Host "Target Cloud : $Environment" -ForegroundColor Cyan
Write-Host "App Name     : $AppName"     -ForegroundColor Cyan
Write-Host ""

$currentCloud = (az cloud show --query name -o tsv 2>$null).Trim()
if ($currentCloud -ne $Environment) {
    Write-Host "Switching az CLI cloud to $Environment ..." -ForegroundColor Yellow
    az cloud set --name $Environment | Out-Null
}

# Verify login
$account = az account show --query "{tenantId:tenantId, sub:id, name:name}" -o json 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged in. Run 'az login' first."
    exit 1
}

Write-Host "Logged in to tenant : $($account.tenantId)" -ForegroundColor Green
Write-Host "Subscription        : $($account.name)"     -ForegroundColor Green
Write-Host ""

# ─── Required Graph API Application permissions ──────────────────
# These are the minimum permissions a Teams Calling Bot needs.
# All are Application permissions (no user sign-in required).
#
# Microsoft Graph App ID is always 00000003-0000-0000-c000-000000000000
# Permission GUIDs are stable across clouds.
$graphAppId = '00000003-0000-0000-c000-000000000000'

$requiredPermissions = @(
    @{ id = '284383ee-7f6e-4e40-a2a8-e85dcb029101'; type = 'Role'; name = 'Calls.Initiate.All'           },
    @{ id = '4c277553-8a09-487b-8023-29ee378d8324'; type = 'Role'; name = 'Calls.InitiateGroupCall.All'  },
    @{ id = 'f6b49018-60ab-4f81-83bd-22caeabfed2d'; type = 'Role'; name = 'Calls.JoinGroupCall.All'      },
    @{ id = 'fd7ccf6b-3d28-418b-9701-cd10f5cd2fd4'; type = 'Role'; name = 'Calls.JoinGroupCallAsGuest.All'},
    @{ id = 'a7a681dc-756e-4909-b988-f160edc6655f'; type = 'Role'; name = 'Calls.AccessMedia.All'        }
)

# Build the requiredResourceAccess JSON for az ad app create
$resourceAccess = $requiredPermissions | ForEach-Object {
    @{ id = $_.id; type = $_.type }
}

$requiredResourceAccess = @(
    @{
        resourceAppId  = $graphAppId
        resourceAccess = $resourceAccess
    }
) | ConvertTo-Json -Depth 5 -Compress

# ─── Create the App Registration ────────────────────────────────
Write-Host "Creating App Registration '$AppName' ..." -ForegroundColor Cyan

$app = az ad app create `
    --display-name $AppName `
    --sign-in-audience 'AzureADMyOrg' `
    --required-resource-accesses $requiredResourceAccess `
    -o json | ConvertFrom-Json

$appId   = $app.appId
$appObjId = $app.id

Write-Host "  App Registration created" -ForegroundColor Green
Write-Host "  Application (client) ID : $appId"    -ForegroundColor White
Write-Host "  Object ID               : $appObjId" -ForegroundColor White

# ─── Create Service Principal ────────────────────────────────────
Write-Host ""
Write-Host "Creating Service Principal ..." -ForegroundColor Cyan

$sp = az ad sp create --id $appId -o json 2>$null | ConvertFrom-Json
if (-not $sp) {
    Write-Warning "Service Principal may already exist. Fetching..."
    $sp = az ad sp show --id $appId -o json | ConvertFrom-Json
}
Write-Host "  Service Principal created (object ID: $($sp.id))" -ForegroundColor Green

# ─── Create Client Secret ────────────────────────────────────────
Write-Host ""
Write-Host "Creating client secret (expires in $SecretYears year(s)) ..." -ForegroundColor Cyan

$secretEndDate = (Get-Date).AddYears($SecretYears).ToString("yyyy-MM-ddTHH:mm:ssZ")

$secretResult = az ad app credential reset `
    --id $appId `
    --display-name $SecretDisplayName `
    --end-date $secretEndDate `
    --append `
    -o json | ConvertFrom-Json

$clientSecret = $secretResult.password
Write-Host "  Client secret created. Expires: $secretEndDate" -ForegroundColor Green

# ─── Grant Admin Consent ─────────────────────────────────────────
Write-Host ""
Write-Host "Granting admin consent for Graph API permissions ..." -ForegroundColor Cyan
Write-Host "  (This requires Global Administrator or Privileged Role Administrator)" -ForegroundColor Yellow

try {
    az ad app permission admin-consent --id $appId 2>&1 | Out-Null
    Write-Host "  Admin consent granted." -ForegroundColor Green
}
catch {
    Write-Warning "Admin consent could not be granted automatically."
    Write-Warning "Go to Azure Portal > Entra ID > App Registrations > '$AppName'"
    Write-Warning "> API Permissions > Grant admin consent for your tenant"
}

# ─── Get Tenant ID ───────────────────────────────────────────────
$tenantId = $account.tenantId

# ─── Output Summary ──────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  App Registration Complete — Copy these into bicepparam    " -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "  param teamsBotAppId      = '$appId'"      -ForegroundColor Yellow
Write-Host "  param teamsBotAppPassword = '$clientSecret'" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Tenant ID (azureAdTenantId, already set): $tenantId" -ForegroundColor White
Write-Host ""
Write-Host "  ⚠  IMPORTANT: Save the secret NOW — it cannot be retrieved again." -ForegroundColor Red
Write-Host "  ⚠  Update infra/parameters/azuregov.bicepparam with the values above." -ForegroundColor Red
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Next step: Deploy infra/main.bicep with the updated parameters," -ForegroundColor Cyan
Write-Host "then register the bot in Teams Admin Center." -ForegroundColor Cyan
