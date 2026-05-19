# Azure Communication Services - Custom Domain Quick Reference

## The Problem
You're trying to configure Direct Routing in Azure Communication Services, but the portal won't let you proceed until you configure a custom domain with DNS verification.

## What's the Custom Domain For?

The custom domain (e.g., `acs.contoso.com` or `sip.contoso.com`) is used as the **SIP domain identity** for call routing:

- Your SBC routes calls to this domain instead of directly to Azure
- Azure verifies you own the domain before allowing Direct Routing
- It becomes part of SIP URIs in call flows (e.g., `sip:user@acs.contoso.com`)

## Quick Steps

### 1. Choose a Subdomain
Pick a subdomain of your organization's domain:
- ✅ Good: `acs.contoso.com`, `sip.contoso.com`, `ivr.contoso.com`
- ❌ Bad: `contoso.com` (use subdomain, not root)

### 2. Add Domain to ACS
**Portal Method:**
- Azure Portal → Your ACS Resource → Domains → Add domain
- Enter your subdomain → Azure shows DNS records

**Script Method:**
```powershell
cd c:\DSOP\repos3\IVR\e911-ivr\scripts

.\Configure-AcsCustomDomain.ps1 `
    -ResourceGroup "your-resource-group" `
    -AcsResourceName "your-acs-resource-name" `
    -CustomDomain "acs.contoso.com"
```

### 3. Get DNS Verification Records
Azure will provide 2 TXT records like:

| Type | Name | Value |
|------|------|-------|
| TXT | `_dnsauth.acs.contoso.com` | `ms-domain-verification=abc123...` |
| TXT | `acs.contoso.com` | `ms-acscustom-domain-verification=xyz789...` |

### 4. Add DNS Records
**If using Azure DNS:**
```powershell
# Automated
.\Configure-AcsCustomDomain.ps1 `
    -ResourceGroup "ivr-rg" `
    -AcsResourceName "your-acs-name" `
    -CustomDomain "acs.contoso.com" `
    -DnsResourceGroup "dns-rg" `
    -DnsZoneName "contoso.com" `
    -AutoAddDnsRecords
```

**If using another DNS provider (GoDaddy, Cloudflare, etc.):**
1. Log into DNS provider
2. Find DNS management for your domain
3. Add both TXT records with exact values from Azure
4. Save changes

### 5. Wait for DNS Propagation
- Usually takes 5-60 minutes
- Can take up to 48 hours in rare cases
- Test with: `Resolve-DnsName -Name "acs.contoso.com" -Type TXT`

### 6. Verify in Azure
**Portal Method:**
- Azure Portal → ACS Resource → Domains → Your domain → Verify

**Script Method:**
The script automatically attempts verification

### 7. Success!
Once verified, you can now:
- ✅ Configure Direct Routing
- ✅ Add your SBC
- ✅ Create voice routing policies

## Files Created

| File | Purpose |
|------|---------|
| `docs/acs-configuration-guide.md` | Complete step-by-step guide |
| `scripts/Configure-AcsCustomDomain.ps1` | Automation script |
| `docs/acs-custom-domain-quickref.md` | This quick reference |

## Common Issues

### "DNS records not found"
- **Wait longer** - DNS takes time to propagate
- **Check format** - Some providers need just `acs`, others need `acs.contoso.com`
- **Verify value** - Copy exact value from Azure (don't add quotes)

### "Domain already in use"
- Each domain can only be verified in one ACS resource
- Remove from other ACS resource first

### "Verification failed"
- Run `Resolve-DnsName -Name "acs.contoso.com" -Type TXT` to verify records are visible
- Check that TXT value exactly matches what Azure provided
- Try verification again after waiting longer

## What to Do After Verification

Once your domain is verified, continue with the ACS Configuration Guide:

1. **Step 4**: Configure Direct Routing in ACS Portal
   - Add your SBC FQDN
   - Create voice routing policies
   
2. **Step 5**: Configure your Session Border Controller
   - Point SBC to `sip.pstnhub.microsoft.com` (or `.azure.us` for Gov)
   - Configure TLS on port 5061
   
3. **Step 6-7**: Configure phone numbers in IVR and Avaya

4. **Step 8**: Test end-to-end call flow

See full guide: `docs/acs-configuration-guide.md`

## Quick Test Commands

```powershell
# Test DNS resolution
Resolve-DnsName -Name "acs.contoso.com" -Type TXT

# Check domain status in Azure
az communication show --name "your-acs-name" --resource-group "your-rg"

# List all domains in ACS (requires REST API)
# See Configure-AcsCustomDomain.ps1 for examples
```

## Need Help?

1. Check Application Insights logs for errors
2. Review full guide: `docs/acs-configuration-guide.md`
3. Contact your DNS administrator if DNS records aren't propagating
4. Check Azure portal for detailed error messages

---

**Quick Start Command:**

```powershell
# Navigate to scripts folder
cd c:\DSOP\repos3\IVR\e911-ivr\scripts

# Run configuration script
.\Configure-AcsCustomDomain.ps1 `
    -ResourceGroup "YOUR_RESOURCE_GROUP" `
    -AcsResourceName "YOUR_ACS_RESOURCE_NAME" `
    -CustomDomain "acs.YOUR_DOMAIN.com"
```

Replace:
- `YOUR_RESOURCE_GROUP` - Your Azure resource group name
- `YOUR_ACS_RESOURCE_NAME` - Your ACS resource name (check Azure Portal)
- `YOUR_DOMAIN.com` - Your organization's domain

---

**Last Updated:** May 19, 2026
