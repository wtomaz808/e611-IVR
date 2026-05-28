# Deploy IVR Admin Portal and PSTN Simulator to Azure Web Apps
# Uses PowerShell Az module to avoid Azure CLI terminal lockup issues

param(
    [string]$ResourceGroup = "rg-ivr-dev",
    [string]$AdminAppName = "ivr-dev-admin-4c5ax3aimbdsy",
    [string]$SimulatorAppName = "ivr-dev-simulator-4c5ax3aimbdsy"
)

Write-Host "Starting deployment to Azure Web Apps..." -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Yellow
Write-Host "Admin App: $AdminAppName" -ForegroundColor Yellow
Write-Host "Simulator App: $SimulatorAppName" -ForegroundColor Yellow
Write-Host ""

# Check if Az module is installed
if (!(Get-Module -ListAvailable -Name Az.Websites)) {
    Write-Host "Az.Websites module not found. Installing..." -ForegroundColor Yellow
    Install-Module -Name Az.Websites -Scope CurrentUser -Force -AllowClobber
}

# Check if logged in
try {
    $context = Get-AzContext
    if (!$context) {
        Write-Host "Not logged into Azure. Please run: Connect-AzAccount" -ForegroundColor Red
        exit 1
    }
    Write-Host "Logged in as: $($context.Account.Id)" -ForegroundColor Green
    Write-Host "Subscription: $($context.Subscription.Name)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "Not logged into Azure. Please run: Connect-AzAccount" -ForegroundColor Red
    exit 1
}

# Deploy Admin Portal
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Deploying Admin Portal..." -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan

$adminZipPath = "src\IVR.AdminPortal\deploy.zip"
if (!(Test-Path $adminZipPath)) {
    Write-Host "ERROR: Admin Portal deploy.zip not found at: $adminZipPath" -ForegroundColor Red
    Write-Host "Please build the application first." -ForegroundColor Red
    exit 1
}

try {
    Write-Host "Uploading Admin Portal package..." -ForegroundColor Yellow
    Publish-AzWebApp `
        -ResourceGroupName $ResourceGroup `
        -Name $AdminAppName `
        -ArchivePath (Resolve-Path $adminZipPath).Path `
        -Force
    
    Write-Host "✓ Admin Portal deployed successfully!" -ForegroundColor Green
    Write-Host "  URL: https://$AdminAppName.azurewebsites.us" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to deploy Admin Portal: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Deploy PSTN Simulator
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Deploying PSTN Simulator..." -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan

$simulatorZipPath = "simulator\src\PstnSimulator\deploy.zip"
if (!(Test-Path $simulatorZipPath)) {
    Write-Host "ERROR: PSTN Simulator deploy.zip not found at: $simulatorZipPath" -ForegroundColor Red
    Write-Host "Please build the application first." -ForegroundColor Red
    exit 1
}

try {
    Write-Host "Uploading PSTN Simulator package..." -ForegroundColor Yellow
    Publish-AzWebApp `
        -ResourceGroupName $ResourceGroup `
        -Name $SimulatorAppName `
        -ArchivePath (Resolve-Path $simulatorZipPath).Path `
        -Force
    
    Write-Host "✓ PSTN Simulator deployed successfully!" -ForegroundColor Green
    Write-Host "  URL: https://$SimulatorAppName.azurewebsites.us" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to deploy PSTN Simulator: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Admin Portal: https://$AdminAppName.azurewebsites.us" -ForegroundColor Cyan
Write-Host "PSTN Simulator: https://$SimulatorAppName.azurewebsites.us" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: It may take 1-2 minutes for the apps to fully start after deployment." -ForegroundColor Yellow
