# Quick Reference: ACS Setup Strategy
## For Azure Government with MSFT FTE Tenant

---

## ✅ Confirmed Finding

**Phone number purchasing is NOT available** in your Azure Government subscription.

```
Error: "The requested operation is not supported in this environment"
```

---

## 🎯 Your Three Options

### 1️⃣ Direct Routing (Production Ready)

**Best for:** Real Avaya integration & production deployment

**What you need:**
- Domain name (~$12-20/year)
- DNS access (registrar or Azure DNS)
- Session Border Controller access

**Time to setup:** 1-2 hours

**Steps:**
```powershell
# 1. Purchase domain (e.g., witomasitest.us)
# 2. Update config
cd C:\DSOP\repos3\IVR\e611-ivr\scripts
code acs-config-params.ps1  # Update CustomDomain

# 3. Run setup
.\Run-AcsConfiguration.ps1

# 4. Add DNS records when prompted
# 5. Configure SBC
```

**Docs:** [`docs/acs-configuration-guide.md`](../docs/acs-configuration-guide.md)

---

### 2️⃣ PSTN Simulator (Quick Testing)

**Best for:** Testing IVR logic without real phone setup

**What you need:**
- Docker Desktop
- 5 minutes

**Time to setup:** 5 minutes

**Steps:**
```powershell
cd C:\DSOP\repos3\IVR\e611-ivr\simulator
docker-compose up
```

Open browser: http://localhost:5000

**Docs:** [`docs/pstn-simulator.md`](../docs/pstn-simulator.md)

---

### 3️⃣ Commercial Azure (Testing Alternative)

**Best for:** Testing with real phone numbers without SBC

**What you need:**
- Personal Azure subscription (Commercial cloud)
- $1-2/month for test phone number

**Time to setup:** 30 minutes

**Steps:**
1. Create subscription in Azure Commercial (azure.com)
2. Deploy ACS resource
3. Purchase test phone number
4. Test call automation
5. Migrate learnings to Azure Gov

---

## 📋 Decision Guide

| Your Goal | Recommended Option |
|-----------|-------------------|
| Test IVR logic quickly | Option 2: PSTN Simulator |
| Need real phone integration now | Option 3: Commercial Azure |
| Production deployment planning | Option 1: Direct Routing |
| Avaya CM integration testing | Option 1: Direct Routing |

---

## 🚀 Recommended Next Step

**For immediate testing:** Use the PSTN Simulator

```powershell
# Start simulator
cd C:\DSOP\repos3\IVR\e611-ivr\simulator
docker-compose up

# In another terminal, ensure your Function App is running
cd C:\DSOP\repos3\IVR\e611-ivr
func start --csharp
```

Then test your IVR without any phone number setup!

---

## 📞 Domain Purchase Guide (for Direct Routing)

If you choose Direct Routing, here's how to get a domain:

### Quick Domain Shopping List

| Registrar | Domain | Cost/Year | Notes |
|-----------|--------|-----------|-------|
| **Namecheap** | .us | ~$12 | Good value, easy DNS |
| **GoDaddy** | .us | ~$15 | Most popular, reliable |
| **Google Domains** | .com | ~$12 | Simple interface |
| **Cloudflare** | .com | ~$10 | Free DNS included |

**Suggested domain names:**
- `witomasitest.us`
- `ivr-dev.us`
- `dsopivr.com`
- `witomasidev.com`

### After Purchase

1. **Option A: Keep DNS at Registrar (Easier)**
   - Leave default nameservers
   - Add TXT records manually when script provides them

2. **Option B: Move DNS to Azure (Automated)**
   ```powershell
   # Create Azure DNS zone
   az network dns zone create `
       --resource-group "rg-ivr-dev" `
       --name "yourdomain.us"
   
   # Get nameservers
   az network dns zone show `
       --resource-group "rg-ivr-dev" `
       --name "yourdomain.us" `
       --query nameServers
   
   # Update nameservers at your registrar
   ```

---

## 📚 All Documentation

- **Phone Number Strategy:** [`docs/acs-phone-number-strategy.md`](../docs/acs-phone-number-strategy.md)
- **ACS Configuration:** [`docs/acs-configuration-guide.md`](../docs/acs-configuration-guide.md)
- **PSTN Simulator:** [`docs/pstn-simulator.md`](../docs/pstn-simulator.md)
- **Azure Gov Status:** [`docs/azure-gov-direct-routing-limitation.md`](../docs/azure-gov-direct-routing-limitation.md)

---

## 🔧 Scripts Available

```powershell
cd C:\DSOP\repos3\IVR\e611-ivr\scripts

# Check phone number availability
.\Check-AcsPhoneNumberAvailability.ps1 `
    -ResourceGroup "rg-ivr-dev" `
    -AcsResourceName "ivr-dev-acs-bld64pwxb4ukq"

# Configure custom domain (after getting domain)
.\Configure-AcsCustomDomain.ps1 `
    -ResourceGroup "rg-ivr-dev" `
    -AcsResourceName "ivr-dev-acs-bld64pwxb4ukq" `
    -CustomDomain "acs.yourdomain.us"

# Or use the wrapper
.\Run-AcsConfiguration.ps1
```

---

## ❓ Need Help?

1. **Check the docs** folder for detailed guides
2. **Run the availability script** to see current status
3. **Start with the simulator** for quick testing
4. **Plan Direct Routing** for production readiness
