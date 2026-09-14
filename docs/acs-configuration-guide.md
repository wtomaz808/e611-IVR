# Azure Communication Services Configuration Guide
## e611-IVR with Avaya Call Manager Integration

---

## Overview

This guide walks through configuring Azure Communication Services (ACS) to receive calls from your Avaya Call Manager system. Your deployment uses **Direct Routing**, where phone numbers assigned in Avaya are routed through a Session Border Controller (SBC) to Azure Communication Services, triggering your IVR Function App.

### Architecture Flow

```
Avaya Call Manager
    ↓ (Assigns phone number)
Customer Dials → PSTN Carrier → Avaya CM
    ↓ (SIP Trunk)
Session Border Controller (SBC)
    ↓ (Direct Routing)
Azure Communication Services (ACS)
    ↓ (Event Grid Event)
Azure Functions (IVR Engine)
```

---

## Prerequisites

Before starting, ensure you have:

- ✅ Azure Communication Services resource deployed (already done)
- ✅ Azure subscription with appropriate permissions
- ✅ A certified SBC (AudioCodes, Ribbon, Oracle, etc.) or access to configure your existing SBC
- ✅ Phone number(s) assigned in Avaya Call Manager
- ✅ SBC fully qualified domain name (FQDN) and network access
- ✅ TLS certificate for SBC (signed by a trusted CA)
- ✅ **A domain name you own with DNS management access** (required for Direct Routing)
- ✅ Permission to add DNS TXT records to your domain

---

## Step 1: Configure Custom Domain for Direct Routing

**IMPORTANT**: This step is a prerequisite for Direct Routing. You must complete it before configuring your SBC in Azure Communication Services.

### What is the Custom Domain Used For?

Azure Communication Services Direct Routing uses a custom domain as the **SIP domain identity** for routing calls. This domain:

1. **Establishes trust** - Azure verifies you own the domain before allowing SIP routing
2. **Creates SIP URIs** - Used in SIP signaling between your SBC and Azure (e.g., `user@contoso.com`)
3. **Enables routing** - Your SBC routes calls to this domain instead of directly to Azure endpoints
4. **Provides identity** - Acts as the domain portion of SIP addresses in call flows

### Domain Selection Guidelines

Choose a subdomain of your organization's domain:

| ✅ Recommended | ❌ Not Recommended |
|---------------|-------------------|
| `acs.contoso.com` | `contoso.com` (root domain) |
| `sip.contoso.com` | `azure.com` (not your domain) |
| `ivr.contoso.com` | `contoso.net` (unless you own it) |
| `voice.contoso.com` | Public domains (gmail.com, etc.) |

**Best Practice**: Use a dedicated subdomain like `acs.contoso.com` or `sip.contoso.com` to keep it separate from other services.

### Step 1.1: Gather Domain Information

Before starting, identify:

| Item | Example | Where to Find |
|------|---------|---------------|
| **Your Organization Domain** | `contoso.com` | Your company's primary domain |
| **Chosen Subdomain** | `acs.contoso.com` | Subdomain you'll use for ACS |
| **DNS Provider** | GoDaddy, Cloudflare, Azure DNS | Where your domain's DNS is managed |
| **DNS Management Access** | Admin credentials | Required to add TXT records |

### Step 1.2: Add Domain to Azure Communication Services

#### Using Azure Portal

1. Navigate to **Azure Portal** → Your ACS resource
2. In the left menu, select **Domains**
3. Click **Add domain** or **Connect domain**
4. Enter your subdomain (e.g., `acs.contoso.com`)
5. Click **Add** or **Next**

Azure will generate DNS verification records that you need to add to your DNS provider.

#### Using PowerShell

```powershell
# Set variables
$resourceGroup = "your-resource-group-name"
$acsResourceName = "ivr-dev-acs-abc123"
$customDomain = "acs.contoso.com"

# Add domain to ACS (this initiates the verification process)
# Note: Direct ACS domain management via Azure CLI is limited
# We'll use Azure REST API via PowerShell

# Get ACS resource details
$acsResource = az communication list --resource-group $resourceGroup --query "[?name=='$acsResourceName']" | ConvertFrom-Json

# Get access token
$token = az account get-access-token --resource https://management.azure.com/ --query accessToken -o tsv

# Add custom domain
$subscriptionId = (az account show --query id -o tsv)
$apiVersion = "2023-04-01"
$uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Communication/communicationServices/$acsResourceName/domains/$customDomain`?api-version=$apiVersion"

$body = @{
    properties = @{
        domainManagement = "CustomerManaged"
    }
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$response = Invoke-RestMethod -Uri $uri -Method Put -Headers $headers -Body $body

Write-Host "Domain added. Please add the following DNS records to verify ownership:" -ForegroundColor Green
$response.properties.verificationRecords
```

### Step 1.3: Get DNS Verification Records

After adding the domain, Azure provides DNS verification records.

#### Using Azure Portal

1. In your ACS resource, go to **Domains**
2. Click on your newly added domain
3. You'll see verification status and required DNS records
4. Note the **TXT record** values (usually 2 records)

Example verification records you'll see:

| Type | Host/Name | Value |
|------|-----------|-------|
| TXT | `_dnsauth.acs.contoso.com` | `ms-domain-verification=abc123def456...` |
| TXT | `acs.contoso.com` | `ms-acscustom-domain-verification=xyz789...` |

#### Using PowerShell to Retrieve Records

```powershell
# Get domain verification records
$uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Communication/communicationServices/$acsResourceName/domains/$customDomain`?api-version=$apiVersion"

$response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers

Write-Host "DNS Verification Records:" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Yellow

foreach ($record in $response.properties.verificationRecords) {
    Write-Host "Type: $($record.type)" -ForegroundColor Cyan
    Write-Host "Name: $($record.name)" -ForegroundColor Cyan
    Write-Host "Value: $($record.value)" -ForegroundColor Cyan
    Write-Host "TTL: $($record.ttl)" -ForegroundColor Cyan
    Write-Host "--------------------------------"
}
```

### Step 1.4: Add DNS Records to Your DNS Provider

Now add the TXT records to your DNS provider. Instructions vary by provider:

#### Option A: Azure DNS

If your domain is managed in Azure DNS:

```powershell
# Variables
$zoneName = "contoso.com"  # Your root domain
$dnsResourceGroup = "dns-resource-group"  # Where your DNS zone is
$recordName = "_dnsauth.acs"  # Subdomain for verification
$txtValue = "ms-domain-verification=abc123def456..."  # From Azure ACS portal

# Add first TXT record (_dnsauth)
az network dns record-set txt add-record `
    --resource-group $dnsResourceGroup `
    --zone-name $zoneName `
    --record-set-name $recordName `
    --value $txtValue

# Add second TXT record (main subdomain)
$recordName2 = "acs"
$txtValue2 = "ms-acscustom-domain-verification=xyz789..."

az network dns record-set txt add-record `
    --resource-group $dnsResourceGroup `
    --zone-name $zoneName `
    --record-set-name $recordName2 `
    --value $txtValue2

Write-Host "DNS records added successfully. DNS propagation may take 5-60 minutes." -ForegroundColor Green
```

#### Option B: GoDaddy, Cloudflare, or Other Provider

1. Log into your DNS provider's control panel
2. Find DNS management for `contoso.com`
3. Add TXT records:

**Record 1:**
- Type: `TXT`
- Host/Name: `_dnsauth.acs` (or `_dnsauth.acs.contoso.com` depending on provider)
- Value: `ms-domain-verification=abc123def456...`
- TTL: `3600` (1 hour)

**Record 2:**
- Type: `TXT`
- Host/Name: `acs` (or `acs.contoso.com`)
- Value: `ms-acscustom-domain-verification=xyz789...`
- TTL: `3600`

4. Save changes

### Step 1.5: Verify DNS Records

Wait for DNS propagation (5-60 minutes), then verify the records are visible:

```powershell
# Test DNS resolution
$domain = "acs.contoso.com"

Write-Host "Testing DNS propagation..." -ForegroundColor Yellow

# Check _dnsauth record
Write-Host "`nChecking _dnsauth.$domain..." -ForegroundColor Cyan
Resolve-DnsName -Name "_dnsauth.$domain" -Type TXT | Select-Object Name, Type, Strings

# Check main domain record
Write-Host "`nChecking $domain..." -ForegroundColor Cyan
Resolve-DnsName -Name $domain -Type TXT | Select-Object Name, Type, Strings

Write-Host "`nIf you see the ms-domain-verification and ms-acscustom-domain-verification values, DNS is ready!" -ForegroundColor Green
```

### Step 1.6: Verify Domain in Azure Communication Services

Once DNS records are propagated, verify the domain in Azure:

#### Using Azure Portal

1. Go to your ACS resource → **Domains**
2. Select your domain
3. Click **Verify** or **Verify domain**
4. Azure will check the DNS records
5. Status should change from "Pending" to "Verified"

#### Using PowerShell

```powershell
# Trigger domain verification
$uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Communication/communicationServices/$acsResourceName/domains/$customDomain/verifyDomainOwnership?api-version=$apiVersion"

Write-Host "Attempting to verify domain..." -ForegroundColor Yellow

try {
    $verifyResponse = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers
    
    Write-Host "Verification initiated successfully!" -ForegroundColor Green
    
    # Check verification status
    Start-Sleep -Seconds 10
    $statusUri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Communication/communicationServices/$acsResourceName/domains/$customDomain`?api-version=$apiVersion"
    $status = Invoke-RestMethod -Uri $statusUri -Method Get -Headers $headers
    
    Write-Host "Domain Status: $($status.properties.domainVerificationStatus)" -ForegroundColor Cyan
    
    if ($status.properties.domainVerificationStatus -eq "Verified") {
        Write-Host "✅ Domain verified successfully! You can now configure Direct Routing." -ForegroundColor Green
    } else {
        Write-Host "⏳ Verification pending. Check DNS records and try again in a few minutes." -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Verification failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Common causes:" -ForegroundColor Yellow
    Write-Host "  - DNS records not yet propagated (wait longer)" -ForegroundColor Yellow
    Write-Host "  - Incorrect TXT record values" -ForegroundColor Yellow
    Write-Host "  - DNS records added to wrong domain/subdomain" -ForegroundColor Yellow
}
```

### Step 1.7: Troubleshooting Domain Verification

#### DNS Records Not Found

**Problem**: Azure can't find your DNS records during verification

**Solutions**:
1. **Wait longer** - DNS propagation can take up to 48 hours (usually 5-60 minutes)
2. **Check record format** - Some DNS providers require different formats:
   - Some want just `acs` as the host
   - Others want the full FQDN `acs.contoso.com`
   - Try both if verification fails
3. **Use DNS lookup tools**:
   ```powershell
   # Check from multiple DNS servers
   Resolve-DnsName -Name "acs.contoso.com" -Type TXT -Server 8.8.8.8  # Google DNS
   Resolve-DnsName -Name "acs.contoso.com" -Type TXT -Server 1.1.1.1  # Cloudflare DNS
   ```

#### Wrong TXT Value

**Problem**: DNS records exist but verification still fails

**Solution**: Copy the exact TXT value from Azure portal - don't add quotes or modify it

#### Multiple TXT Records

**Problem**: You already have TXT records on the subdomain

**Solution**: Most DNS providers support multiple TXT records - add the Azure verification as an additional TXT record, don't replace existing ones

#### Domain Already Verified in Another ACS Resource

**Problem**: Error stating domain is in use

**Solution**: Each custom domain can only be verified in **one** ACS resource. Remove it from the other resource first.

### Automated Setup Script

Here's a complete PowerShell script to automate the entire process:

```powershell
<#
.SYNOPSIS
    Configure custom domain for Azure Communication Services Direct Routing
.DESCRIPTION
    Adds a custom domain to ACS, retrieves verification records, optionally adds them to Azure DNS,
    and verifies the domain.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$true)]
    [string]$AcsResourceName,
    
    [Parameter(Mandatory=$true)]
    [string]$CustomDomain,
    
    [Parameter(Mandatory=$false)]
    [string]$DnsResourceGroup = "",
    
    [Parameter(Mandatory=$false)]
    [string]$DnsZoneName = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoAddDnsRecords
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "ACS Custom Domain Configuration" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Get Azure context
$subscriptionId = (az account show --query id -o tsv)
$token = az account get-access-token --resource https://management.azure.com/ --query accessToken -o tsv

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$apiVersion = "2023-04-01"

# Step 1: Add domain to ACS
Write-Host "Step 1: Adding domain to Azure Communication Services..." -ForegroundColor Yellow
$uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Communication/communicationServices/$AcsResourceName/domains/$CustomDomain`?api-version=$apiVersion"

$body = @{
    properties = @{
        domainManagement = "CustomerManaged"
    }
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $uri -Method Put -Headers $headers -Body $body
    Write-Host "✅ Domain added successfully" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Host "⚠️ Domain already exists, retrieving existing configuration..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers
    } else {
        throw $_
    }
}

Start-Sleep -Seconds 5

# Step 2: Get verification records
Write-Host "`nStep 2: Retrieving DNS verification records..." -ForegroundColor Yellow
$getUri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Communication/communicationServices/$AcsResourceName/domains/$CustomDomain`?api-version=$apiVersion"
$domainInfo = Invoke-RestMethod -Uri $getUri -Method Get -Headers $headers

Write-Host "`nDNS Records Required:" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan

$verificationRecords = @()
foreach ($record in $domainInfo.properties.verificationRecords) {
    Write-Host ""
    Write-Host "Type: $($record.type)" -ForegroundColor White
    Write-Host "Name: $($record.name)" -ForegroundColor White
    Write-Host "Value: $($record.value)" -ForegroundColor White
    Write-Host "TTL: $($record.ttl)" -ForegroundColor White
    
    $verificationRecords += $record
}

# Step 3: Add DNS records (if requested and using Azure DNS)
if ($AutoAddDnsRecords -and $DnsResourceGroup -and $DnsZoneName) {
    Write-Host "`nStep 3: Adding DNS records to Azure DNS..." -ForegroundColor Yellow
    
    foreach ($record in $verificationRecords) {
        if ($record.type -eq "TXT") {
            # Extract subdomain from full name
            $recordName = $record.name -replace "\.$DnsZoneName$", ""
            
            Write-Host "Adding TXT record: $recordName" -ForegroundColor Cyan
            
            az network dns record-set txt add-record `
                --resource-group $DnsResourceGroup `
                --zone-name $DnsZoneName `
                --record-set-name $recordName `
                --value $record.value `
                --ttl $record.ttl `
                --output none
            
            Write-Host "✅ Record added" -ForegroundColor Green
        }
    }
    
    Write-Host "`nWaiting 60 seconds for DNS propagation..." -ForegroundColor Yellow
    Start-Sleep -Seconds 60
} else {
    Write-Host "`n⚠️ Please add the above DNS records to your DNS provider manually." -ForegroundColor Yellow
    Write-Host "After adding, press Enter to continue with verification..." -ForegroundColor Yellow
    Read-Host
}

# Step 4: Test DNS propagation
Write-Host "`nStep 4: Testing DNS propagation..." -ForegroundColor Yellow

foreach ($record in $verificationRecords) {
    if ($record.type -eq "TXT") {
        try {
            $dnsResult = Resolve-DnsName -Name $record.name -Type TXT -ErrorAction Stop
            $foundValue = $dnsResult | Where-Object { $_.Strings -contains $record.value }
            
            if ($foundValue) {
                Write-Host "✅ DNS record verified: $($record.name)" -ForegroundColor Green
            } else {
                Write-Host "❌ DNS record not found: $($record.name)" -ForegroundColor Red
                Write-Host "   Expected value: $($record.value)" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "❌ Could not resolve: $($record.name)" -ForegroundColor Red
        }
    }
}

# Step 5: Verify domain in Azure
Write-Host "`nStep 5: Verifying domain in Azure Communication Services..." -ForegroundColor Yellow
$verifyUri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Communication/communicationServices/$AcsResourceName/domains/$CustomDomain/verifyDomainOwnership?api-version=$apiVersion"

try {
    $verifyResponse = Invoke-RestMethod -Uri $verifyUri -Method Post -Headers $headers
    Write-Host "✅ Verification initiated" -ForegroundColor Green
    
    Start-Sleep -Seconds 10
    
    # Check status
    $statusResponse = Invoke-RestMethod -Uri $getUri -Method Get -Headers $headers
    $status = $statusResponse.properties.domainVerificationStatus
    
    Write-Host "`nVerification Status: $status" -ForegroundColor Cyan
    
    if ($status -eq "Verified") {
        Write-Host "`n✅✅✅ SUCCESS! Domain verified!" -ForegroundColor Green
        Write-Host "You can now configure Direct Routing for this domain." -ForegroundColor Green
    } else {
        Write-Host "`n⏳ Verification is pending or failed." -ForegroundColor Yellow
        Write-Host "Check the Azure portal for details, or wait and run verification again." -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Verification failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "Configuration Complete" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
```

**Usage Example**:

```powershell
# Manual DNS management
.\Configure-AcsCustomDomain.ps1 `
    -ResourceGroup "ivr-rg" `
    -AcsResourceName "ivr-dev-acs-abc123" `
    -CustomDomain "acs.contoso.com"

# Automatic DNS management (Azure DNS only)
.\Configure-AcsCustomDomain.ps1 `
    -ResourceGroup "ivr-rg" `
    -AcsResourceName "ivr-dev-acs-abc123" `
    -CustomDomain "acs.contoso.com" `
    -DnsResourceGroup "dns-rg" `
    -DnsZoneName "contoso.com" `
    -AutoAddDnsRecords
```

### What Happens After Verification?

Once your domain is verified:

1. ✅ The "Direct Routing" section in Azure portal becomes accessible
2. ✅ You can add SBCs using your custom domain
3. ✅ Your SBC will route calls to your domain (e.g., `sip:user@acs.contoso.com`)
4. ✅ Azure Communication Services will accept calls for this domain

---

## Step 2: Obtain Azure Communication Services Connection Details

First, get your ACS resource information:

### Using Azure Portal

1. Navigate to **Azure Portal** → **Resource Groups** → your resource group
2. Find your Communication Services resource (name pattern: `ivr-{env}-acs-{suffix}`)
3. Click on the resource
4. Note the following:
   - **Resource Name**
   - **Data Location** (e.g., `United States` or `usgov` for Azure Government)
   - **Hostname** (found under Overview)

### Using Azure CLI

```powershell
# Get ACS resource details
az communication list --resource-group <your-rg-name> --output table

# Get the connection string (store securely)
az communication list-key --name <acs-resource-name> --resource-group <your-rg-name>
```

### Required Information

| Item | Where to Find | Example |
|------|---------------|---------|
| **ACS Resource ID** | Portal → Resource → Properties | `/subscriptions/.../communicationServices/ivr-dev-acs-abc123` |
| **Connection String** | Keys section (already configured in your Function App) | `endpoint=https://...;accesskey=...` |
| **Hostname** | Overview page | `ivr-dev-acs-abc123.communication.azure.com` |

---

## Step 3: Configure Event Grid Subscription

Your IVR receives incoming calls via Event Grid. The subscription should already be deployed via Bicep, but verify it's configured correctly.

### Verify Existing Event Grid Subscription

```powershell
# List Event Grid subscriptions for your ACS resource
az eventgrid event-subscription list --source-resource-id "<acs-resource-id>" --output table
```

### Required Event Grid Configuration

| Setting | Value |
|---------|-------|
| **Event Type** | `Microsoft.Communication.IncomingCall` |
| **Endpoint Type** | `Azure Function` |
| **Endpoint** | `https://<your-function-app>.azurewebsites.net/api/incoming-call` |
| **Authentication** | Function Key (Level: Anonymous recommended for Event Grid) |

### Create Event Grid Subscription (if missing)

```powershell
# Get your Function App URL
$functionAppUrl = "https://ivr-dev-func-abc123.azurewebsites.net"
$acsResourceId = "/subscriptions/xxx/resourceGroups/xxx/providers/Microsoft.Communication/communicationServices/ivr-dev-acs-abc123"

# Create subscription
az eventgrid event-subscription create `
    --name "acs-incoming-calls" `
    --source-resource-id $acsResourceId `
    --endpoint "$functionAppUrl/api/incoming-call" `
    --endpoint-type "webhook" `
    --included-event-types "Microsoft.Communication.IncomingCall"
```

### Validate Event Grid Subscription

Event Grid will send a `SubscriptionValidationEvent` to your endpoint. Your `IncomingCallHandler` function already handles this validation automatically (see lines 69-73 in `IncomingCallHandler.cs`).

Check Function App logs to confirm validation succeeded:

```powershell
# Stream Function App logs
az monitor app-insights query `
    --app <app-insights-resource-name> `
    --analytics-query "traces | where message contains 'SubscriptionValidationEvent' | top 10 by timestamp desc"
```

---

## Step 4: Configure Direct Routing in Azure Communication Services

Now configure ACS to accept calls from your SBC.

**PREREQUISITE**: Ensure you completed Step 1 and have a verified custom domain before proceeding.

### Step 4.1: Identify Your SBC Details

Gather this information from your network/telephony team:

| Item | Example | Notes |
|------|---------|-------|
| **SBC FQDN** | `sbc.contoso.com` | Must have valid TLS certificate |
| **SBC Public IP** | `203.0.113.45` | For firewall rules |
| **SIP Signaling Port** | `5067` | Standard TLS port for SIP |
| **SBC Vendor** | AudioCodes, Ribbon, Oracle | Must be Microsoft-certified |
| **Phone Numbers** | `+15551234567` | E.164 format |

### Step 4.2: Add SBC to Azure Communication Services

**Note**: This uses your verified custom domain from Step 1.

#### Using Azure Portal

1. Go to **Azure Portal** → Your ACS resource
2. Navigate to **Telephony** → **Direct Routing**
3. Click **Add SBC**
4. Enter:
   - **FQDN**: Your SBC fully qualified domain name (e.g., `sbc.contoso.com`)
   - **Port**: 5067 (default) or your SBC's TLS SIP port
5. Click **Save**

#### Using Azure CLI (Recommended for Azure Government)

```powershell
# Add SBC to ACS Direct Routing
az communication direct-routing add-sbc `
    --communication-service "<acs-resource-name>" `
    --resource-group "<resource-group-name>" `
    --sbc-fqdn "sbc.contoso.com" `
    --sip-signaling-port 5067
```

### Step 4.3: Create Voice Routing Policy

Voice routing determines which phone numbers route through your SBC to the IVR.

```powershell
# Create voice route
az communication direct-routing voice-route create `
    --communication-service "<acs-resource-name>" `
    --resource-group "<resource-group-name>" `
    --name "ivr-inbound-route" `
    --number-pattern "^\+1555123456[0-9]$" `
    --priority 1 `
    --sbcs "sbc.contoso.com"
```

| Parameter | Description | Example |
|-----------|-------------|---------|
| `number-pattern` | Regex matching called numbers | `^\+1555.*$` (all +1555 numbers) |
| `priority` | Route priority (lower = higher priority) | `1` |
| `sbcs` | Comma-separated list of SBC FQDNs | `sbc.contoso.com,sbc2.contoso.com` |

---

## Step 5: Configure Your Session Border Controller (SBC)

Your SBC must be configured to route calls to Azure Communication Services.

### Step 5.1: SBC Configuration Requirements

Configure your SBC with these settings:

| Setting | Value |
|---------|-------|
| **Destination SIP Domain** | `sip.pstnhub.microsoft.com` (commercial Azure)<br>or `sip.pstnhub.azure.us` (Azure Government) |
| **Transport Protocol** | TLS |
| **Port** | 5061 |
| **SIP Trunk FQDN** | Your SBC FQDN (`sbc.contoso.com`) |
| **Certificate** | Valid TLS certificate from trusted CA (not self-signed) |
| **Media Bypass** | Optional (recommended for lower latency) |

### Step 5.2: SBC Trunk Configuration (Example: AudioCodes)

```
# SIP Interface Configuration
SIP Interface Settings:
  Network Interface: WAN
  Transport Type: TLS
  TLS Context: Azure_TLS_Context
  Listen Port: 5067

# Proxy Set for Azure ACS
Proxy Set:
  Name: Azure_ACS_Trunk
  Proxy Address: sip.pstnhub.microsoft.com  # or .azure.us for Gov Cloud
  Transport Type: TLS
  Proxy Port: 5061
  Enable Keep-Alive: Yes

# IP Group (Azure ACS)
IP Group:
  Type: Server
  Proxy Set: Azure_ACS_Trunk
  SBC IPv4 SIP Interface: <your-sbc-wan-interface>

# Routing - Outbound to Azure ACS
Outbound Call Routing:
  Rule: To_Azure_IVR
  Destination Pattern: .* 
  Source IP Group: <Avaya_CM_IP_Group>
  Destination IP Group: Azure_ACS_IP_Group
```

### Step 5.3: Firewall & Network Configuration

Ensure network connectivity:

**Outbound from SBC to Azure** (required):

| Direction | Protocol | Port | Destination |
|-----------|----------|------|-------------|
| Outbound | TCP | 5061 | `sip.pstnhub.microsoft.com` or `.azure.us` |
| Outbound | UDP | 3478-3481 | Media endpoints (Azure IP ranges) |

**Inbound to SBC from Azure** (optional - for Media Bypass):

| Direction | Protocol | Port | Source |
|-----------|----------|------|--------|
| Inbound | TCP | 5061 | Azure ACS IP ranges |
| Inbound | UDP | 49152-65535 | Azure ACS IP ranges |

**Azure IP Ranges**: Download from [Azure IP Ranges and Service Tags](https://www.microsoft.com/download/details.aspx?id=56519)

### Step 5.4: TLS Certificate Requirements

Your SBC must present a valid TLS certificate:

- ✅ Issued by a trusted Certificate Authority (Digicert, GlobalSign, etc.)
- ✅ Subject Name or SAN includes your SBC FQDN
- ✅ Not expired
- ❌ Self-signed certificates NOT supported

---

## Step 6: Configure Phone Number in IVR System

Now configure your IVR to recognize and route calls to the correct menu.

### Step 6.1: Add Phone Number Configuration

Create a `PhoneNumberConfig` document in Cosmos DB:

#### Using Azure Portal

1. Navigate to **Azure Portal** → **Cosmos DB** → your IVR database
2. Open **Data Explorer**
3. Navigate to `ivr-db` → `PhoneNumbers` container
4. Click **New Item**
5. Paste the following JSON (customize for your number):

```json
{
  "id": "pn-e611-main-001",
  "phoneNumber": "+15551234567",
  "label": "e611 Main Emergency Line",
  "numberType": "DirectRouting",
  "rootMenuId": "menu-e611-root",
  "businessHoursConfigId": "bh-24x7",
  "welcomePromptId": "prompt-e611-welcome",
  "sbcFqdn": "sbc.contoso.com",
  "sbcPort": 5067,
  "calledNumberAliases": [],
  "isActive": true,
  "partitionKey": "phone-number"
}
```

6. Click **Save**

#### Field Descriptions

| Field | Required | Description |
|-------|----------|-------------|
| `id` | Yes | Unique identifier (use pattern: `pn-{purpose}-###`) |
| `phoneNumber` | Yes | E.164 format phone number |
| `label` | Yes | Human-readable name for admin portal |
| `numberType` | Yes | `"DirectRouting"` for Avaya integration |
| `rootMenuId` | Optional | Specific menu for this number (falls back to system default if null) |
| `businessHoursConfigId` | Optional | Business hours schedule |
| `welcomePromptId` | Optional | Initial prompt played when call connects |
| `sbcFqdn` | Optional | Override SBC FQDN (uses system default if null) |
| `sbcPort` | Optional | Override SBC port (uses system default 5067 if null) |
| `calledNumberAliases` | No | Additional called-number identifiers this config applies to |
| `isActive` | Yes | `true` to enable, `false` to disable without deleting |
| `partitionKey` | Yes | Always `"phone-number"` |

### Step 6.2: Configure System-Wide PSTN Settings

Update your `SystemConfig` in Cosmos DB:

1. Navigate to **Data Explorer** → `Config` container
2. Find the document with `id: "system-config"`
3. Update these fields:

```json
{
  "id": "system-config",
  "pstnMode": "DirectRouting",
  "defaultSbcFqdn": "sbc.contoso.com",
  "defaultSbcPort": 5067,
  "enableDisconnectTransferToVdn": false,
  "defaultCm10Vdn": null,
  "cm10SbcFqdn": null,
  "cm10SbcPort": null,
  "cm10TransferPromptId": null,
  ...
}
```

| Field | Value for Your Setup |
|-------|---------------------|
| `pstnMode` | `"DirectRouting"` |
| `defaultSbcFqdn` | Your SBC FQDN |
| `defaultSbcPort` | `5067` (or your SBC port) |

---

## Step 7: Configure Avaya Call Manager

Configure Avaya to route calls to your SBC, which then sends them to Azure.

### Step 7.1: Create SIP Trunk in Avaya CM

1. Log into **Avaya System Manager**
2. Navigate to **Communication Manager** → **Trunks**
3. Create new **SIP Trunk Group**:
   - **Group Number**: e.g., `100`
   - **Group Type**: `sip`
   - **Service Type**: `public-network`
   - **Signaling Group**: (assign to your SBC signaling group)
   - **Incoming Calling Party**: `public`
   - **Outgoing Calling Party**: `public`

### Step 7.2: Configure Trunk Member

Add trunk member pointing to your SBC:

```
add trunk-group <trunk-group-number> member

Trunk Group Member:
  Member Number: 1
  Port: <network-region>
  Connected Signaling-Group: <sbc-sig-group>
  Near-end Node Name: <procr>
  Far-end Node Name: <SBC-node-name>
```

### Step 7.3: Route Pattern to IVR

Create a route pattern to send calls to the IVR:

```
change route-pattern <pattern-number>

Route Pattern:
  Pattern Name: TO_IVR_ACS
  Pattern Number: <number>
  Application: <app-name>
  Grp No: <trunk-group-created-above>
  FRL: <appropriate-FRL>
```

### Step 7.4: Assign DID to Route Pattern

Map your phone number to the route pattern:

```
change public-unknown-numbering 0

Matching Pattern    Trunk Group    Total Call Received
+15551234567        100           -
```

---

## Step 8: Test the Configuration

### Step 8.1: Enable Diagnostic Logging

Before testing, enable detailed logging:

```powershell
# Enable Application Insights logging
az monitor app-insights component update `
    --app <app-insights-name> `
    --resource-group <resource-group> `
    --query-access Enabled
```

### Step 8.2: Test Call Flow

1. **Place a test call** from an external phone to your configured number
2. **Monitor logs** in real-time:

```powershell
# Stream Function App logs
az webapp log tail --name <function-app-name> --resource-group <resource-group>
```

### Step 8.3: Verify Each Step

Check these logs to confirm proper flow:

| Step | Expected Log Message | Function |
|------|---------------------|----------|
| 1. Call arrives at ACS | `IncomingCallHandler received request` | IncomingCallHandler |
| 2. Event Grid received | `eventType: Microsoft.Communication.IncomingCall` | IncomingCallHandler |
| 3. Number extracted | `Call from +1555... to +1555...` | IncomingCallHandler |
| 4. ANI/ALI lookup | `ANI/ALI lookup for caller` | AniAliService |
| 5. Call answered | `Call answered successfully. Connection ID: ...` | IncomingCallHandler |
| 6. Callback received | `CallConnected event received` | CallbackHandler |
| 7. Menu played | `Playing menu prompt` | CallFlowEngine |

### Step 8.4: Common Issues & Troubleshooting

#### Issue: No IncomingCall Event Received

**Symptoms**: No logs in IncomingCallHandler after placing call

**Possible Causes**:
- Event Grid subscription not configured or validation failed
- SBC not routing to Azure ACS
- Voice routing policy doesn't match called number

**Solution**:
```powershell
# Verify Event Grid subscription
az eventgrid event-subscription show `
    --name "acs-incoming-calls" `
    --source-resource-id "<acs-resource-id>"

# Check provisioning state (should be "Succeeded")
```

#### Issue: Call Rejected or Not Answered

**Symptoms**: IncomingCall event received, but call not answered

**Possible Causes**:
- Phone number not configured in `PhoneNumbers` container
- `isActive: false` on phone number config
- Blocked caller in ANI database

**Solution**:
```powershell
# Query Cosmos DB for phone number config
az cosmosdb sql query `
    --account-name <cosmos-account> `
    --database-name ivr-db `
    --container-name PhoneNumbers `
    --query-text "SELECT * FROM c WHERE c.phoneNumber = '+15551234567'"
```

#### Issue: SIP URI Parsing Error

**Symptoms**: Error log: `Failed to extract phone number from participant`

**Possible Causes**:
- SBC sending non-standard SIP URI format
- Caller ID not properly formatted

**Solution**: Check the `IncomingCallHandler` logs for the raw `from.rawId` value and update the `ExtractPhoneFromUri` method in `AniAliService` if needed.

#### Issue: Audio Not Playing

**Symptoms**: Call connects but no prompts heard

**Possible Causes**:
- Cognitive Services not configured
- Blob Storage connection issue
- Invalid prompt ID

**Solution**:
```powershell
# Verify Cognitive Services endpoint in Function App settings
az functionapp config appsettings list `
    --name <function-app-name> `
    --resource-group <resource-group> `
    --query "[?name=='CognitiveServicesEndpoint'].value"
```

---

## Step 9: Monitor and Maintain

### Real-Time Monitoring

Monitor call flow in Application Insights:

```kusto
// Recent incoming calls
traces
| where message contains "IncomingCallHandler"
| where timestamp > ago(1h)
| project timestamp, message, severityLevel
| order by timestamp desc

// Failed call attempts
traces
| where severityLevel >= 3  // Warning or Error
| where message contains "Call" or message contains "ACS"
| where timestamp > ago(24h)
| project timestamp, message, severityLevel
| order by timestamp desc
```

### Health Checks

Create an automated health check:

```powershell
# Check ACS Direct Routing status
az communication direct-routing list-sbcs `
    --communication-service "<acs-resource-name>" `
    --resource-group "<resource-group-name>"
```

### Regular Maintenance Tasks

| Task | Frequency | Action |
|------|-----------|--------|
| **SBC Certificate Renewal** | Before expiry | Update SBC TLS certificate and verify connectivity |
| **Call Log Review** | Daily | Check Cosmos DB `CallLogs` container for errors |
| **Event Grid Health** | Weekly | Verify subscription status and delivery success rate |
| **Voice Route Testing** | After changes | Test call from external number |

---

## Step 10: Advanced Configuration (Optional)

### Enable Avaya CM10 VDN Integration

If you want to transfer calls back to Avaya after IVR self-service:

1. **Update SystemConfig**:
```json
{
  "enableDisconnectTransferToVdn": true,
  "defaultCm10Vdn": "sip:70100@sbc.contoso.com",
  "cm10SbcFqdn": "sbc.contoso.com",
  "cm10SbcPort": 5067,
  "cm10TransferPromptId": "prompt-transfer-hold"
}
```

2. **Create transfer prompts** in Cosmos DB `Prompts` container

3. **Test transfer** by completing IVR flow and verifying handoff to Avaya

### Multiple Phone Numbers with Different Menus

To route different numbers to different IVR menu trees:

1. Create separate `PhoneNumberConfig` documents for each number
2. Set different `rootMenuId` values
3. Each number will enter its own menu tree upon call arrival

Example:

```json
// Emergency line → e611 menu
{
  "phoneNumber": "+15551234567",
  "rootMenuId": "menu-e611-root"
}

// Support line → General support menu
{
  "phoneNumber": "+15551234999",
  "rootMenuId": "menu-support-root"
}
```

---

## Configuration Checklist

Use this checklist to track your progress:

### Azure Configuration
- [ ] Custom domain verified (Step 1)
- [ ] ACS resource deployed and keys obtained
- [ ] Event Grid subscription created and validated
- [ ] SBC added to ACS Direct Routing
- [ ] Voice routing policy created
- [ ] Function App callback URL accessible

### SBC Configuration
- [ ] SBC trunk configured for Azure ACS
- [ ] TLS certificate installed and valid
- [ ] Network connectivity verified (ports 5061, 3478-3481)
- [ ] Firewall rules configured

### Avaya Configuration
- [ ] SIP trunk group created in CM
- [ ] Trunk member pointing to SBC
- [ ] Route pattern configured
- [ ] DID assigned to route pattern

### IVR Application Configuration
- [ ] Phone number added to `PhoneNumbers` container
- [ ] `SystemConfig` PSTN mode set to `DirectRouting`
- [ ] SBC FQDN and port configured
- [ ] Root menu and prompts created
- [ ] Test call successful

---

## Additional Resources

- [Azure Communication Services Direct Routing Documentation](https://learn.microsoft.com/azure/communication-services/concepts/telephony-sms/direct-routing-infrastructure)
- [Certified SBC List](https://learn.microsoft.com/azure/communication-services/concepts/telephony-sms/direct-routing-provisioning)
- [Event Grid Event Schema](https://learn.microsoft.com/azure/communication-services/concepts/call-automation/incoming-call-notification)
- [Project Architecture Documentation](architecture.md)
- [System Integration Guide](system-integration.md)

---

## Support

For issues or questions:

1. Check Application Insights logs
2. Review SBC and Avaya CM logs
3. Verify network connectivity between components
4. Contact your network/telephony team for SBC and Avaya configuration support

---

**Document Version**: 1.0  
**Last Updated**: May 19, 2026  
**Maintained By**: IVR Development Team
