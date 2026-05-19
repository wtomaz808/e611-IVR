# IVR Scripts

This folder contains PowerShell automation scripts for the E911 IVR system.

## Available Scripts

### Configure-AcsCustomDomain.ps1

Automates the Azure Communication Services custom domain configuration required for Direct Routing.

**Purpose:** Direct Routing requires a verified custom domain. This script adds the domain to ACS, retrieves DNS verification records, optionally adds them to Azure DNS, and verifies the domain.

**Usage:**

```powershell
# Manual DNS management (you add records to your DNS provider)
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

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `ResourceGroup` | Yes | Azure resource group containing your ACS resource |
| `AcsResourceName` | Yes | Name of your ACS resource |
| `CustomDomain` | Yes | Subdomain to configure (e.g., `acs.contoso.com`) |
| `DnsResourceGroup` | No | Resource group for Azure DNS zone |
| `DnsZoneName` | No | Azure DNS zone name (e.g., `contoso.com`) |
| `AutoAddDnsRecords` | No | Switch to auto-add DNS records (Azure DNS only) |

**Prerequisites:**
- Azure CLI installed and authenticated (`az login`)
- Owner or Contributor role on the ACS resource
- DNS management access (if using `-AutoAddDnsRecords`)

**See Also:**
- Full guide: [docs/acs-configuration-guide.md](../docs/acs-configuration-guide.md)
- Quick reference: [docs/acs-custom-domain-quickref.md](../docs/acs-custom-domain-quickref.md)

---

## Future Scripts

Additional automation scripts will be added here as the project evolves.
