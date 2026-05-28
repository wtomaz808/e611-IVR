<#
.SYNOPSIS
    Check Azure Communication Services phone number availability and capabilities

.DESCRIPTION
    This script helps determine if your Azure subscription and ACS resource can purchase
    and use native phone numbers. Useful for testing ACS call automation without Direct Routing.

.PARAMETER ResourceGroup
    The Azure resource group containing your ACS resource

.PARAMETER AcsResourceName
    The name of your Azure Communication Services resource

.EXAMPLE
    .\Check-AcsPhoneNumberAvailability.ps1 `
        -ResourceGroup "rg-ivr-dev" `
        -AcsResourceName "ivr-dev-acs-bld64pwxb4ukq"

.NOTES
    File Name      : Check-AcsPhoneNumberAvailability.ps1
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
    [string]$AcsResourceName
)

# ─────────────────────────────────────────────────────────────────
# Functions
# ─────────────────────────────────────────────────────────────────
function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-TestResult {
    param(
        [string]$Test,
        [string]$Status,
        [string]$Message
    )
    
    $statusSymbol = switch ($Status) {
        "Pass" { "✅" }
        "Fail" { "❌" }
        "Warning" { "⚠️" }
        "Info" { "ℹ️" }
        default { "•" }
    }
    
    $color = switch ($Status) {
        "Pass" { "Green" }
        "Fail" { "Red" }
        "Warning" { "Yellow" }
        "Info" { "Cyan" }
        default { "White" }
    }
    
    Write-Host "$statusSymbol $Test" -ForegroundColor $color
    if ($Message) {
        Write-Host "   $Message" -ForegroundColor Gray
    }
}

# ─────────────────────────────────────────────────────────────────
# Main Script
# ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     ACS Phone Number Availability Check                  ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

# ─────────────────────────────────────────────────────────────────
# Test 1: Azure CLI and Authentication
# ─────────────────────────────────────────────────────────────────
Write-Section "Step 1: Validating Azure Environment"

try {
    $azVersion = az version | ConvertFrom-Json
    Write-TestResult "Azure CLI Installed" "Pass" "Version: $($azVersion.'azure-cli')"
} catch {
    Write-TestResult "Azure CLI Installed" "Fail" "Azure CLI not found"
    exit 1
}

try {
    $account = az account show | ConvertFrom-Json
    Write-TestResult "Azure Authentication" "Pass" "Logged in as: $($account.user.name)"
    Write-Host "   Subscription: $($account.name)" -ForegroundColor Gray
    Write-Host "   Tenant: $($account.tenantId)" -ForegroundColor Gray
} catch {
    Write-TestResult "Azure Authentication" "Fail" "Not logged in - run 'az login'"
    exit 1
}

# Detect cloud environment
$cloud = az cloud show | ConvertFrom-Json
$cloudName = $cloud.name
Write-TestResult "Cloud Environment" "Info" $cloudName

if ($cloudName -eq "AzureUSGovernment") {
    Write-Host "   Note: Phone number availability may be limited in Azure Government" -ForegroundColor Yellow
}

# ─────────────────────────────────────────────────────────────────
# Test 2: ACS Resource Validation
# ─────────────────────────────────────────────────────────────────
Write-Section "Step 2: Validating ACS Resource"

try {
    $acsResource = az communication show `
        --resource-group $ResourceGroup `
        --name $AcsResourceName `
        --query "{name:name, location:location, dataLocation:properties.dataLocation, provisioningState:properties.provisioningState}" `
        --output json | ConvertFrom-Json
    
    Write-TestResult "ACS Resource Found" "Pass" $AcsResourceName
    Write-Host "   Location: $($acsResource.location)" -ForegroundColor Gray
    Write-Host "   Data Location: $($acsResource.dataLocation)" -ForegroundColor Gray
    Write-Host "   Status: $($acsResource.provisioningState)" -ForegroundColor Gray
} catch {
    Write-TestResult "ACS Resource Found" "Fail" "Cannot find ACS resource"
    exit 1
}

# Get connection string
try {
    $connectionString = az communication list-key `
        --resource-group $ResourceGroup `
        --name $AcsResourceName `
        --query primaryConnectionString -o tsv
    
    Write-TestResult "ACS Connection String" "Pass" "Retrieved successfully"
} catch {
    Write-TestResult "ACS Connection String" "Fail" "Cannot retrieve connection string"
    exit 1
}

# ─────────────────────────────────────────────────────────────────
# Test 3: Check Existing Phone Numbers
# ─────────────────────────────────────────────────────────────────
Write-Section "Step 3: Checking Existing Phone Numbers"

try {
    $existingNumbers = az communication phonenumber list `
        --connection-string $connectionString `
        2>&1
    
    if ($LASTEXITCODE -ne 0) {
        # Check for specific error messages
        if ($existingNumbers -match "not supported in this environment") {
            Write-TestResult "Phone Number Operations" "Fail" "NOT SUPPORTED in this environment"
            Write-Host ""
            Write-Host "   FINDING: Phone number operations are restricted in this subscription." -ForegroundColor Red
            Write-Host "   This could be due to:" -ForegroundColor Yellow
            Write-Host "     • Azure Government cloud limitations" -ForegroundColor Yellow
            Write-Host "     • Microsoft FTE tenant restrictions" -ForegroundColor Yellow
            Write-Host "     • Subscription policy blocking phone number purchases" -ForegroundColor Yellow
            $phoneNumbersSupported = $false
        } elseif ($existingNumbers -match "Forbidden") {
            Write-TestResult "Phone Number Operations" "Fail" "Access Forbidden"
            $phoneNumbersSupported = $false
        } else {
            Write-TestResult "Phone Number Operations" "Warning" "Unknown error occurred"
            Write-Host "   Error: $existingNumbers" -ForegroundColor Gray
            $phoneNumbersSupported = $false
        }
    } else {
        $numbers = $existingNumbers | ConvertFrom-Json
        if ($numbers.Count -gt 0) {
            Write-TestResult "Existing Phone Numbers" "Pass" "Found $($numbers.Count) number(s)"
            foreach ($number in $numbers) {
                Write-Host "   📞 $($number.phoneNumber) ($($number.phoneNumberType))" -ForegroundColor Green
            }
        } else {
            Write-TestResult "Existing Phone Numbers" "Info" "No numbers currently assigned"
        }
        $phoneNumbersSupported = $true
    }
} catch {
    Write-TestResult "Phone Number Operations" "Fail" "Error checking phone numbers"
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    $phoneNumbersSupported = $false
}

# ─────────────────────────────────────────────────────────────────
# Test 4: Try to Search for Available Numbers (if supported)
# ─────────────────────────────────────────────────────────────────
if ($phoneNumbersSupported) {
    Write-Section "Step 4: Searching for Available Phone Numbers"
    
    Write-Host "Checking toll-free number availability (this may take a moment)..." -ForegroundColor Cyan
    
    try {
        $searchResult = az communication phonenumber search `
            --connection-string $connectionString `
            --phone-number-type "tollFree" `
            --assignment-type "application" `
            --capabilities "calling:outbound,calling:inbound" `
            --area-code "800" `
            --quantity 1 `
            2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-TestResult "Toll-Free Numbers Available" "Pass" "Phone numbers are available for purchase"
            $search = $searchResult | ConvertFrom-Json
            Write-Host "   Search ID: $($search.searchId)" -ForegroundColor Gray
            Write-Host "   Available Numbers: $($search.phoneNumbers.Count)" -ForegroundColor Gray
        } else {
            Write-TestResult "Toll-Free Numbers Available" "Warning" "Unable to search for numbers"
            Write-Host "   Response: $searchResult" -ForegroundColor Gray
        }
    } catch {
        Write-TestResult "Toll-Free Numbers Available" "Warning" "Search operation failed"
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
}

# ─────────────────────────────────────────────────────────────────
# Summary and Recommendations
# ─────────────────────────────────────────────────────────────────
Write-Section "Summary and Recommendations"

Write-Host ""

if ($phoneNumbersSupported) {
    Write-Host "✅ GOOD NEWS: Phone numbers ARE supported in your environment!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Purchase a phone number using the Azure Portal or CLI" -ForegroundColor White
    Write-Host "  2. Configure the number for incoming calls in Event Grid" -ForegroundColor White
    Write-Host "  3. Test your IVR call flow without needing Direct Routing" -ForegroundColor White
    Write-Host ""
    Write-Host "To purchase a toll-free number:" -ForegroundColor Yellow
    Write-Host "  az communication phonenumber search \" -ForegroundColor Magenta
    Write-Host "    --connection-string '$connectionString' \" -ForegroundColor Magenta
    Write-Host "    --phone-number-type 'tollFree' \" -ForegroundColor Magenta
    Write-Host "    --assignment-type 'application' \" -ForegroundColor Magenta
    Write-Host "    --capabilities 'calling:inbound,calling:outbound' \" -ForegroundColor Magenta
    Write-Host "    --area-code '800' \" -ForegroundColor Magenta
    Write-Host "    --quantity 1" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "See docs/acs-native-phone-numbers.md for detailed instructions" -ForegroundColor Cyan
} else {
    Write-Host "❌ FINDING: Phone numbers are NOT supported in your environment" -ForegroundColor Red
    Write-Host ""
    Write-Host "Why This Happens:" -ForegroundColor Yellow
    Write-Host "  • Azure Government has different feature availability than commercial Azure" -ForegroundColor White
    Write-Host "  • Microsoft FTE tenants may have restrictions on phone number purchases" -ForegroundColor White
    Write-Host "  • Subscription policies may block telephony services" -ForegroundColor White
    Write-Host ""
    Write-Host "Alternative Options:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Option 1: Use Direct Routing (requires DNS domain)" -ForegroundColor Yellow
    Write-Host "  • Purchase a domain name (e.g., witomasitest.us)" -ForegroundColor White
    Write-Host "  • Configure custom domain with ACS" -ForegroundColor White
    Write-Host "  • Set up Session Border Controller (SBC)" -ForegroundColor White
    Write-Host "  • Connect Avaya Call Manager" -ForegroundColor White
    Write-Host "  See: docs/acs-configuration-guide.md" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Option 2: Test with Commercial Azure (if available)" -ForegroundColor Yellow
    Write-Host "  • Create a personal Azure subscription in commercial cloud" -ForegroundColor White
    Write-Host "  • Deploy ACS resource there for testing" -ForegroundColor White
    Write-Host "  • Phone numbers are widely available in commercial Azure" -ForegroundColor White
    Write-Host ""
    Write-Host "Option 3: Use PSTN Simulator for Development" -ForegroundColor Yellow
    Write-Host "  • Your project includes a PSTN Simulator (see simulator/ folder)" -ForegroundColor White
    Write-Host "  • Simulates incoming calls without real phone numbers" -ForegroundColor White
    Write-Host "  • Perfect for development and testing IVR logic" -ForegroundColor White
    Write-Host "  Run: docker-compose up in the simulator folder" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Option 4: Request Access from Microsoft" -ForegroundColor Yellow
    Write-Host "  • Contact Azure Communication Services team" -ForegroundColor White
    Write-Host "  • Request phone number access for your subscription" -ForegroundColor White
    Write-Host "  • Mention you're testing E911 IVR in Azure Government" -ForegroundColor White
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              Availability Check Complete                 ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Return status code based on phone number support
if ($phoneNumbersSupported) {
    exit 0
} else {
    exit 2  # Exit code 2 indicates phone numbers not supported (different from error)
}
