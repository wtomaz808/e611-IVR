# Azure Communication Services Phone Number Strategy
## E911 IVR Development Plan

**Last Updated:** May 27, 2026  
**Environment:** Azure US Government  
**Status:** Phone number operations not supported in current environment

---

## Executive Summary

After testing, we've determined that **native phone number purchasing is NOT available** in the Azure Government subscription being used for this project. This is likely due to:

1. **Azure Government Limitations** - Feature parity differences between commercial and government clouds
2. **Microsoft FTE Tenant Restrictions** - Internal tenant policies
3. **Subscription Policies** - Telephony service restrictions

---

## Testing Results

### What We Tested

```powershell
# Attempted to list phone numbers
az communication phonenumber list --connection-string <connection-string>

# Result: "The requested operation is not supported in this environment"
```

### Error Analysis

| Test | Result | Meaning |
|------|--------|---------|
| ACS Resource Deployment | ✅ Success | Basic ACS works in Azure Gov |
| Call Automation SDK | ✅ Success | Confirmed working in code |
| Event Grid Integration | ✅ Success | Confirmed working in code |
| Phone Number List/Search | ❌ Not Supported | Cannot purchase ACS native numbers |

---

## Recommended Strategy: Use Direct Routing

Since native phone numbers aren't available, proceed with **Direct Routing** as originally planned.

### Prerequisites

#### 1. Acquire a Domain Name

**Option A: Purchase a Test Domain (Recommended)**
- Cost: ~$12-20/year
- Registrars: GoDaddy, Namecheap, Google Domains
- Suggested names:
  - `witomasitest.us`
  - `ivr-dev.us`
  - `dsop-ivr.com`

**Option B: Use Existing Company Domain**
- If your organization has a domain, use a subdomain
- Example: `acs.company.com`
- Requires DNS management access

#### 2. DNS Management

**Option A: Keep DNS at Registrar**
- Simplest approach
- Manually add TXT records when script provides them

**Option B: Move DNS to Azure DNS**
- Better Azure integration
- Automated record creation via script
- Additional cost: ~$0.50/month

```powershell
# Create Azure DNS zone
az network dns zone create `
    --resource-group "rg-ivr-dev" `
    --name "yourdomain.com"

# Get nameservers to update at registrar
az network dns zone show `
    --resource-group "rg-ivr-dev" `
    --name "yourdomain.com" `
    --query nameServers
```

#### 3. Session Border Controller (SBC)

For production Direct Routing, you need an SBC. Options:

**Testing/Development:**
- Use your existing Avaya SBC (if available)
- Software SBC in Azure VM (AudioCodes VE, etc.)
- Partner with SBC vendor for trial

**Production:**
- Certified SBC from: AudioCodes, Ribbon, Oracle, Cisco
- See: https://docs.microsoft.com/azure/communication-services/concepts/telephony-sip

---

## Implementation Plan

### Phase 1: Domain Setup (15-30 minutes)

1. **Purchase Domain**
   ```bash
   # Go to registrar website
   # Purchase: witomasitest.us (or similar)
   # Enable auto-renewal
   ```

2. **Decide on DNS Management**
   - Registrar DNS (manual records) → Skip to Step 4
   - Azure DNS (automated) → Continue to Step 3

3. **Setup Azure DNS (Optional)**
   ```powershell
   # Create DNS zone
   az network dns zone create `
       --resource-group "rg-ivr-dev" `
       --name "witomasitest.us"
   
   # Update nameservers at registrar with Azure DNS nameservers
   ```

4. **Update Configuration Files**
   ```powershell
   # Edit: scripts/acs-config-params.ps1
   $AcsParams = @{
       ResourceGroup   = "rg-ivr-dev"
       AcsResourceName = "ivr-dev-acs-bld64pwxb4ukq"
       CustomDomain    = "acs.witomasitest.us"  # Update this
       
       # If using Azure DNS, uncomment:
       # DnsResourceGroup = "rg-ivr-dev"
       # DnsZoneName = "witomasitest.us"
       # AutoAddDnsRecords = $true
   }
   ```

### Phase 2: ACS Custom Domain Configuration (10-20 minutes)

1. **Run Configuration Script**
   ```powershell
   cd C:\DSOP\repos3\IVR\e911-ivr\scripts
   
   # Using parameter file
   .\Run-AcsConfiguration.ps1
   
   # OR directly
   .\Configure-AcsCustomDomain.ps1 `
       -ResourceGroup "rg-ivr-dev" `
       -AcsResourceName "ivr-dev-acs-bld64pwxb4ukq" `
       -CustomDomain "acs.witomasitest.us"
   ```

2. **Add DNS Records**
   - Script will display TXT records
   - Add them to your DNS provider
   - Wait 5-60 minutes for propagation

3. **Verify Domain**
   - Script will test DNS propagation
   - Verify in Azure portal
   - Status should change to "Verified"

### Phase 3: SBC Configuration (30-60 minutes)

1. **Configure SBC**
   - Add ACS as SIP peer
   - Use verified domain: `acs.witomasitest.us`
   - Configure TLS certificate
   - Test SIP connectivity

2. **Add SBC to ACS**
   - Azure Portal → ACS Resource → Direct Routing
   - Add SBC FQDN
   - Configure voice routing

3. **Test Call Flow**
   - Make test call: Avaya → SBC → ACS → Function App
   - Verify Event Grid events trigger
   - Test IVR functionality

---

## Alternative Options

### Option 1: Use PSTN Simulator (No Phone Number Needed)

Your project includes a PSTN Simulator for testing:

**Advantages:**
- ✅ No domain required
- ✅ No phone number required
- ✅ Perfect for testing IVR logic
- ✅ Already built into your solution

**How to Use:**
```powershell
# Navigate to simulator folder
cd C:\DSOP\repos3\IVR\e911-ivr\simulator

# Start simulator
docker-compose up
```

See: [`docs/pstn-simulator.md`](pstn-simulator.md) for full instructions

### Option 2: Test in Commercial Azure

If you have access to a personal Azure subscription:

1. Deploy ACS resource in commercial Azure
2. Purchase test phone number (~$1-2/month)
3. Test call automation without SBC/Direct Routing
4. Migrate back to Azure Gov for production

### Option 3: Request Microsoft Support

As a Microsoft FTE:

1. File support request for phone number access
2. Explain: Testing E911 IVR in Azure Government
3. Reference: Azure Communication Services phone number restrictions
4. Request: Enable phone number purchase capability

---

## Cost Comparison

| Option | Initial Cost | Monthly Cost | Setup Time |
|--------|-------------|--------------|------------|
| **Direct Routing** | $12-20 (domain) | $0.50 (Azure DNS) | 1-2 hours |
| **PSTN Simulator** | $0 | $0 | 5 minutes |
| **Commercial Azure** | $0 | $1-2 (phone) | 30 minutes |
| **Native Phone (if available)** | $0 | $1-2 (phone) | 15 minutes |

---

## Decision Matrix

| Scenario | Recommended Approach |
|----------|---------------------|
| **Testing IVR logic only** | Use PSTN Simulator |
| **Testing with real calls, no Avaya yet** | Try Commercial Azure |
| **Integration with Avaya CM** | Use Direct Routing |
| **Production deployment** | Direct Routing (required) |

---

## Next Steps

1. **Run the availability check script:**
   ```powershell
   cd C:\DSOP\repos3\IVR\e911-ivr\scripts
   .\Check-AcsPhoneNumberAvailability.ps1 `
       -ResourceGroup "rg-ivr-dev" `
       -AcsResourceName "ivr-dev-acs-bld64pwxb4ukq"
   ```

2. **Choose your strategy:**
   - Quick testing → PSTN Simulator
   - Real integration → Purchase domain and setup Direct Routing

3. **Follow the implementation plan** for your chosen strategy

---

## Related Documentation

- [ACS Configuration Guide](acs-configuration-guide.md) - Full Direct Routing setup
- [PSTN Simulator Guide](pstn-simulator.md) - Testing without phone numbers
- [Azure Gov Direct Routing Status](azure-gov-direct-routing-limitation.md) - Feature availability

---

## Support and Resources

- **Azure Communication Services Docs:** https://docs.microsoft.com/azure/communication-services/
- **Direct Routing Overview:** https://docs.microsoft.com/azure/communication-services/concepts/telephony-sip
- **Certified SBC List:** https://docs.microsoft.com/azure/communication-services/concepts/telephony-sip#session-border-controllers-sbcs

---

**Questions or Issues?** See the main project [README.md](../README.md) or check [docs/](.)
