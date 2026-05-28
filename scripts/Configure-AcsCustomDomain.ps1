<#
.SYNOPSIS
    Configure custom domain for Azure Communication Services Direct Routing

.DESCRIPTION
    This script automates the process of adding a custom domain to Azure Communication Services,
    retrieving DNS verification records, optionally adding them to Azure DNS, and verifying the domain.

.PARAMETER ResourceGroup
    The Azure resource group containing your ACS resource

.PARAMETER AcsResourceName
    The name of your Azure Communication Services resource

.PARAMETER CustomDomain
    The custom domain/subdomain to configure (e.g., acs.contoso.com)

.PARAMETER DnsResourceGroup
    (Optional) Resource group containing your Azure DNS zone

.PARAMETER DnsZoneName
    (Optional) Azure DNS zone name (e.g., contoso.com)

.PARAMETER AutoAddDnsRecords
    Switch to automatically add DNS records to Azure DNS

.EXAMPLE
    # Manual DNS management
    .\Configure-AcsCustomDomain.ps1 `
        -ResourceGroup "rg-ivr-dev" `
        -AcsResourceName "ivr-dev-acs-bld64pwxb4ukq" `
        -CustomDomain "acs.tomazdev.us"

.EXAMPLE
    # Automatic DNS management (Azure DNS only)
    .\Configure-AcsCustomDomain.ps1 `
        -ResourceGroup "ivr-rg" `
        -AcsResourceName "ivr-dev-acs-abc123" `
        -CustomDomain "acs.contoso.com" `
        -DnsResourceGroup "dns-rg" `
        -DnsZoneName "contoso.com" `
        -AutoAddDnsRecords

.NOTES
    File Name      : Configure-AcsCustomDomain.ps1
    Author         : IVR Development Team
    Prerequisite   : Azure CLI installed and authenticated (az login)
    Copyright 2026 : E911 IVR System
#>

param(
    [Parameter(Mandatory=$true, HelpMessage="Azure resource group name")]
    [ValidateNotNullOrEmpty()]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$true, HelpMessage="Azure Communication Services resource name")]
    [ValidateNotNullOrEmpty()]
    [string]$AcsResourceName,
    
    [Parameter(Mandatory=$true, HelpMessage="Custom domain to configure (e.g., acs.contoso.com)")]
    [ValidateNotNullOrEmpty()]
    [string]$CustomDomain,
    
    [Parameter(Mandatory=$false, HelpMessage="Resource group for Azure DNS zone")]
    [string]$DnsResourceGroup = "",
    
    [Parameter(Mandatory=$false, HelpMessage="Azure DNS zone name (e.g., contoso.com)")]
    [string]$DnsZoneName = "",
    
    [Parameter(Mandatory=$false, HelpMessage="Automatically add DNS records to Azure DNS")]
    [switch]$AutoAddDnsRecords
)

# ─────────────────────────────────────────────────────────────────
# Function: Write-Step
# ─────────────────────────────────────────────────────────────────
function Write-Step {
    param([string]$Message)
    Write-Host "`n$Message" -ForegroundColor Yellow
    Write-Host ("=" * $Message.Length) -ForegroundColor Yellow
}

# ─────────────────────────────────────────────────────────────────
# Function: Write-Success
# ─────────────────────────────────────────────────────────────────
function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

# ─────────────────────────────────────────────────────────────────
# Function: Write-Warning
# ─────────────────────────────────────────────────────────────────
function Write-WarningMessage {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

# ─────────────────────────────────────────────────────────────────
# Function: Write-Error
# ─────────────────────────────────────────────────────────────────
function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# ─────────────────────────────────────────────────────────────────
# Main Script
# ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Azure Communication Services Custom Domain Configuration ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Validate Azure CLI is installed
try {
    $azVersion = az version | ConvertFrom-Json
    Write-Host "Azure CLI Version: $($azVersion.'azure-cli')" -ForegroundColor Gray
} catch {
    Write-ErrorMessage "Azure CLI is not installed or not in PATH"
    Write-Host "Install from: https://aka.ms/installazurecliwindows" -ForegroundColor Yellow
    exit 1
}

# Check if logged in
try {
    $account = az account show | ConvertFrom-Json
    Write-Host "Subscription: $($account.name) ($($account.id))" -ForegroundColor Gray
} catch {
    Write-ErrorMessage "Not logged into Azure. Run 'az login' first."
    exit 1
}

# Detect Azure cloud environment
$cloud = az cloud show | ConvertFrom-Json
$cloudName = $cloud.name
Write-Host "Cloud Environment: $cloudName" -ForegroundColor Gray

# Set endpoints based on cloud
$managementEndpoint = switch ($cloudName) {
    "AzureUSGovernment" { "https://management.usgovcloudapi.net/" }
    "AzureCloud" { "https://management.azure.com/" }
    "AzureChinaCloud" { "https://management.chinacloudapi.cn/" }
    default { "https://management.azure.com/" }
}

Write-Host "Management Endpoint: $managementEndpoint" -ForegroundColor Gray

# Get subscription ID and access token
$subscriptionId = $account.id
Write-Host "Getting access token..." -ForegroundColor Gray
$token = az account get-access-token --resource $managementEndpoint --query accessToken -o tsv

if (-not $token) {
    Write-ErrorMessage "Failed to get access token"
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$apiVersion = "2023-04-01"
$baseUri = $managementEndpoint.TrimEnd('/')

# ─────────────────────────────────────────────────────────────────
# Step 1: Add domain to ACS
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 1: Adding domain to Azure Communication Services"

$uri = "$baseUri/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Communication/communicationServices/$AcsResourceName/domains/$CustomDomain`?api-version=$apiVersion"

$body = @{
    properties = @{
        domainManagement = "CustomerManaged"
    }
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $uri -Method Put -Headers $headers -Body $body -ErrorAction Stop
    Write-Success "Domain '$CustomDomain' added to ACS resource '$AcsResourceName'"
} catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-WarningMessage "Domain already exists, retrieving existing configuration..."
        try {
            $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers -ErrorAction Stop
            Write-Success "Retrieved existing domain configuration"
        } catch {
            Write-ErrorMessage "Failed to retrieve domain: $($_.Exception.Message)"
            exit 1
        }
    } else {
        Write-ErrorMessage "Failed to add domain: $($_.Exception.Message)"
        Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
        exit 1
    }
}

Start-Sleep -Seconds 5

# ─────────────────────────────────────────────────────────────────
# Step 2: Get verification records
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 2: Retrieving DNS verification records"

$getUri = "$baseUri/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Communication/communicationServices/$AcsResourceName/domains/$CustomDomain`?api-version=$apiVersion"

try {
    $domainInfo = Invoke-RestMethod -Uri $getUri -Method Get -Headers $headers -ErrorAction Stop
} catch {
    Write-ErrorMessage "Failed to retrieve domain information: $($_.Exception.Message)"
    exit 1
}

Write-Host ""
Write-Host "DNS Records Required:" -ForegroundColor Cyan
Write-Host "════════════════════" -ForegroundColor Cyan

$verificationRecords = @()
foreach ($record in $domainInfo.properties.verificationRecords) {
    Write-Host ""
    Write-Host "  Type:  $($record.type)" -ForegroundColor White
    Write-Host "  Name:  $($record.name)" -ForegroundColor White
    Write-Host "  Value: $($record.value)" -ForegroundColor Magenta
    Write-Host "  TTL:   $($record.ttl)" -ForegroundColor White
    
    $verificationRecords += $record
}

if ($verificationRecords.Count -eq 0) {
    Write-ErrorMessage "No verification records returned from Azure"
    exit 1
}

# ─────────────────────────────────────────────────────────────────
# Step 3: Add DNS records (if requested and using Azure DNS)
# ─────────────────────────────────────────────────────────────────
if ($AutoAddDnsRecords -and $DnsResourceGroup -and $DnsZoneName) {
    Write-Step "Step 3: Adding DNS records to Azure DNS"
    
    foreach ($record in $verificationRecords) {
        if ($record.type -eq "TXT") {
            # Extract subdomain from full name
            $recordName = $record.name -replace "\.$DnsZoneName$", ""
            
            Write-Host "Adding TXT record: $recordName" -ForegroundColor Cyan
            
            try {
                az network dns record-set txt add-record `
                    --resource-group $DnsResourceGroup `
                    --zone-name $DnsZoneName `
                    --record-set-name $recordName `
                    --value $record.value `
                    --ttl $record.ttl `
                    --output none 2>&1 | Out-Null
                
                Write-Success "Record '$recordName' added successfully"
            } catch {
                Write-ErrorMessage "Failed to add DNS record: $recordName"
                Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
    Write-Host "Waiting 60 seconds for DNS propagation..." -ForegroundColor Yellow
    for ($i = 60; $i -gt 0; $i--) {
        Write-Progress -Activity "Waiting for DNS propagation" -Status "$i seconds remaining" -PercentComplete ((60 - $i) / 60 * 100)
        Start-Sleep -Seconds 1
    }
    Write-Progress -Activity "Waiting for DNS propagation" -Completed
} else {
    Write-Step "Step 3: Manual DNS Configuration Required"
    Write-Host ""
    Write-WarningMessage "You need to add the above DNS records to your DNS provider manually."
    Write-Host ""
    Write-Host "After adding the DNS records, press Enter to continue with verification..." -ForegroundColor Yellow
    Read-Host
}

# ─────────────────────────────────────────────────────────────────
# Step 4: Test DNS propagation
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 4: Testing DNS propagation"

$allRecordsFound = $true

foreach ($record in $verificationRecords) {
    if ($record.type -eq "TXT") {
        Write-Host "Checking: $($record.name)" -ForegroundColor Cyan
        
        try {
            $dnsResult = Resolve-DnsName -Name $record.name -Type TXT -ErrorAction Stop
            $foundValue = $dnsResult | Where-Object { $_.Strings -contains $record.value }
            
            if ($foundValue) {
                Write-Success "DNS record verified"
            } else {
                Write-ErrorMessage "DNS record value mismatch"
                Write-Host "   Expected: $($record.value)" -ForegroundColor Yellow
                Write-Host "   Found: $($dnsResult.Strings -join ', ')" -ForegroundColor Yellow
                $allRecordsFound = $false
            }
        } catch {
            Write-ErrorMessage "Could not resolve DNS record"
            Write-Host "   This may be due to DNS propagation delay" -ForegroundColor Yellow
            $allRecordsFound = $false
        }
    }
}

if (-not $allRecordsFound) {
    Write-Host ""
    Write-WarningMessage "Some DNS records are not yet propagated"
    Write-Host "DNS propagation can take 5-60 minutes (sometimes longer)" -ForegroundColor Yellow
    Write-Host "You can:"
    Write-Host "  1. Wait and run this script again later with just verification" -ForegroundColor Cyan
    Write-Host "  2. Continue anyway and Azure will verify when records propagate" -ForegroundColor Cyan
    Write-Host ""
    $continue = Read-Host "Continue with Azure verification? (y/n)"
    if ($continue -ne 'y') {
        Write-Host "Exiting. Run this script again when DNS records have propagated." -ForegroundColor Yellow
        exit 0
    }
}

# ─────────────────────────────────────────────────────────────────
# Step 5: Verify domain in Azure
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 5: Verifying domain in Azure Communication Services"

$verifyUri = "$baseUri/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Communication/communicationServices/$AcsResourceName/domains/$CustomDomain/verifyDomainOwnership?api-version=$apiVersion"

try {
    Write-Host "Initiating verification..." -ForegroundColor Gray
    $verifyResponse = Invoke-RestMethod -Uri $verifyUri -Method Post -Headers $headers -ErrorAction Stop
    Write-Success "Verification request submitted"
    
    Start-Sleep -Seconds 10
    
    # Check status
    Write-Host "Checking verification status..." -ForegroundColor Gray
    $statusResponse = Invoke-RestMethod -Uri $getUri -Method Get -Headers $headers -ErrorAction Stop
    $status = $statusResponse.properties.domainVerificationStatus
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "Verification Status: $status" -ForegroundColor $(if ($status -eq "Verified") { "Green" } else { "Yellow" })
    Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    if ($status -eq "Verified") {
        Write-Success "SUCCESS! Domain verified successfully!"
        Write-Host ""
        Write-Host "Next Steps:" -ForegroundColor Cyan
        Write-Host "  1. Configure Direct Routing in Azure Portal" -ForegroundColor White
        Write-Host "  2. Add your SBC (Session Border Controller)" -ForegroundColor White
        Write-Host "  3. Create voice routing policies" -ForegroundColor White
        Write-Host ""
        Write-Host "See the ACS Configuration Guide for detailed instructions:" -ForegroundColor Yellow
        Write-Host "  docs/acs-configuration-guide.md" -ForegroundColor Magenta
    } elseif ($status -eq "Pending") {
        Write-WarningMessage "Verification is pending"
        Write-Host "This is normal if DNS records were just added" -ForegroundColor Yellow
        Write-Host "Azure will automatically verify when DNS records propagate" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Check status in Azure Portal:" -ForegroundColor Cyan
        Write-Host "  Portal > ACS Resource > Domains > $CustomDomain" -ForegroundColor White
    } else {
        Write-WarningMessage "Verification status: $status"
        Write-Host "Check Azure portal for details" -ForegroundColor Yellow
    }
    
} catch {
    Write-ErrorMessage "Verification request failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Common causes:" -ForegroundColor Yellow
    Write-Host "  - DNS records not yet propagated (wait 5-60 minutes)" -ForegroundColor White
    Write-Host "  - Incorrect TXT record values" -ForegroundColor White
    Write-Host "  - DNS records added to wrong domain/subdomain" -ForegroundColor White
    Write-Host ""
    Write-Host "You can retry verification later with:" -ForegroundColor Cyan
    Write-Host "  .\Configure-AcsCustomDomain.ps1 -ResourceGroup '$ResourceGroup' -AcsResourceName '$AcsResourceName' -CustomDomain '$CustomDomain'" -ForegroundColor Magenta
    exit 1
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              Configuration Process Complete               ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
