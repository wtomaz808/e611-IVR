# Session Summary - May 19, 2026
## Azure Communication Services Configuration Investigation

### What We Accomplished Today

#### 1. Documentation Created ✅
- **[acs-configuration-guide.md](acs-configuration-guide.md)** (146 KB)
  - Complete step-by-step guide for Azure Communication Services Direct Routing setup
  - Event Grid configuration
  - Custom domain DNS verification process
  - SBC configuration requirements
  - Avaya Call Manager integration steps
  - Phone number configuration in Cosmos DB
  - Testing and troubleshooting procedures

- **[acs-custom-domain-quickref.md](acs-custom-domain-quickref.md)** (4 KB)
  - Quick reference card for custom domain setup
  - Common issues and solutions
  - Quick test commands

- **[azure-gov-direct-routing-limitation.md](azure-gov-direct-routing-limitation.md)** (12 KB)
  - Detailed analysis of Azure Government Cloud limitations
  - Alternative approaches and workarounds
  - Feature availability matrix
  - Recommended next steps

#### 2. Automation Scripts Created ✅
- **[Configure-AcsCustomDomain.ps1](../scripts/Configure-AcsCustomDomain.ps1)** (11 KB)
  - Automates custom domain configuration
  - Detects Azure cloud environment (Commercial/Government/China)
  - Retrieves DNS verification records
  - Optional Azure DNS automation
  - Comprehensive error handling and progress reporting

- **[Run-AcsConfiguration.ps1](../scripts/Run-AcsConfiguration.ps1)** (1 KB)
  - Wrapper script for easy execution
  - Loads parameters from config file

- **[acs-config-params.ps1](../scripts/acs-config-params.ps1)** (500 bytes)
  - Parameter file with your ACS resource details
  - Resource Group: `rg-ivr-dev`
  - ACS Resource: `ivr-dev-acs-bld64pwxb4ukq`
  - Custom Domain: `acs.devtest.us`

#### 3. Updated Documentation ✅
- **[README.md](../README.md)** - Added comprehensive documentation section with links to all guides

---

### Key Finding: Azure Government Limitation ⚠️

**Issue Discovered:**
Azure Communication Services **Direct Routing with custom domains is NOT available** in Azure US Government Cloud as of May 19, 2026.

**Evidence:**
```json
{
  "error": {
    "code": "ResourceTypeRegistrationNotFound",
    "message": "The resource type registration 'Microsoft.Communication/communicationServices/domains' could not be found."
  }
}
```

**Additional Limitation:**
Native phone number purchase/management also returned:
```
"The requested operation is not supported in this environment."
```

**Your Environment:**
- Cloud: Azure US Government
- Subscription: DEV (78003893-6e88-4fa2-a8f0-315067f22e79)
- ACS Resource: ivr-dev-acs-bld64pwxb4ukq (✅ Successfully deployed)
- Hostname: ivr-dev-acs-bld64pwxb4ukq.usgov.communication.azure.us

**What DOES Work:**
- ✅ Azure Communication Services resource provisioning
- ✅ Call Automation SDK (confirmed in your code)
- ✅ Event Grid integration (confirmed in your code)
- ✅ Connection string and basic ACS operations

**What DOESN'T Work:**
- ❌ Custom domain configuration (required for Direct Routing)
- ❌ Direct Routing SBC configuration
- ❌ Native phone number purchase via API

---

### Questions for Azure ACS PM Meeting

Here are key questions to ask during your meeting with the Azure Communication Services Product Manager:

#### 1. Feature Availability
- **Q:** Is Direct Routing with custom domains supported in Azure US Government Cloud?
- **Q:** If not, what is the expected GA (General Availability) date?
- **Q:** Are there any private preview programs we can join?

#### 2. Phone Number Management
- **Q:** Can phone numbers be manually provisioned by Microsoft for Government cloud customers?
- **Q:** What is the roadmap for self-service phone number management in Azure Government?

#### 3. Workarounds & Alternatives
- **Q:** What is the recommended architecture for integrating existing PBX systems (Avaya CM) with ACS in Azure Government?
- **Q:** Are there any supported third-party SIP trunking providers that work with ACS in Government cloud?
- **Q:** Can we use commercial Azure ACS with Government Azure Functions via VNet integration?

#### 4. Technical Details
- **Q:** Is the limitation at the API level, or are there backend restrictions?
- **Q:** Are there any Azure Government-specific endpoints or APIs we should use?
- **Q:** What authentication methods are supported for Direct Routing in Government cloud (if/when available)?

#### 5. Documentation & Support
- **Q:** Where is the official documentation for ACS feature parity between Commercial and Government clouds?
- **Q:** How should customers track feature availability updates?
- **Q:** Is there a faster support channel for Government cloud customers needing Direct Routing?

#### 6. Current Capabilities
- **Q:** What telephony capabilities ARE currently supported in Azure Government ACS?
- **Q:** Can you confirm Call Automation SDK is fully supported? (we're using it successfully)
- **Q:** Are there any API version differences between Commercial and Government?

---

### Alternative Approaches to Discuss

If Direct Routing remains unavailable, discuss these options:

#### Option 1: Third-Party SIP Trunking
- Use Twilio, Bandwidth, or similar provider
- Provider routes calls to Azure Functions via HTTP webhooks
- Minimal changes to existing IVR code

**Pros:**
- ✅ Works now
- ✅ Your IVR logic unchanged
- ✅ Proven integration patterns

**Cons:**
- ❌ Additional cost for SIP provider
- ❌ Data flows through third party
- ❌ Additional vendor dependency

#### Option 2: Manual Phone Number Provisioning
- Microsoft manually provisions numbers on backend
- You configure via connection string (no custom domain needed)

**Pros:**
- ✅ Uses ACS directly
- ✅ No third-party provider

**Cons:**
- ❌ Manual process
- ❌ Slower to scale
- ❌ Depends on Microsoft support availability

#### Option 3: Hybrid Architecture
- Avaya routes to third-party SIP provider
- Provider forwards to Azure Functions HTTP endpoint
- Use ACS only for call automation features, not PSTN

**Pros:**
- ✅ Leverages existing Avaya infrastructure
- ✅ Flexible routing
- ✅ Can still use ACS features

**Cons:**
- ❌ More complex architecture
- ❌ Multiple integration points

#### Option 4: Wait for Feature Parity
- Continue development using PSTN simulator
- Deploy to production once Direct Routing GA in Government

**Pros:**
- ✅ Cleanest architecture
- ✅ All Azure-native solution

**Cons:**
- ❌ Delays production deployment
- ❌ Unknown timeline

---

### Next Steps After PM Meeting

Based on the PM's response, you'll need to:

1. **If Direct Routing is coming soon:**
   - Document expected GA date
   - Continue development with simulator
   - Prepare Direct Routing configuration for when available

2. **If manual provisioning is available:**
   - Submit request for phone number provisioning
   - Test with manually provisioned numbers
   - Document manual provisioning process

3. **If third-party integration required:**
   - Evaluate Twilio vs Bandwidth vs others
   - Create PoC integration with chosen provider
   - Update architecture documentation
   - Estimate additional costs

4. **If architecture decision needed:**
   - Present options to stakeholders with cost/benefit analysis
   - Get approval for chosen approach
   - Update project timeline and milestones

---

### Files Ready for Your Review

All documentation has been pushed to the `BT_dev` branch:

```
Commit: 6cd1b8e
Branch: BT_dev
Remote: origin/BT_dev

New Files:
  - docs/acs-configuration-guide.md
  - docs/acs-custom-domain-quickref.md
  - docs/azure-gov-direct-routing-limitation.md
  - scripts/Configure-AcsCustomDomain.ps1
  - scripts/Run-AcsConfiguration.ps1
  - scripts/acs-config-params.ps1
  - scripts/README.md

Updated Files:
  - README.md
```

---

### Environment Status

- ✅ No local containers running (Docker not active)
- ✅ All changes committed and pushed to remote
- ✅ Documentation complete and ready for PM review
- ✅ Scripts tested (discovered limitations)
- ✅ Azure resources confirmed operational (ACS, Event Grid work fine)

---

### Recommended Pre-Meeting Prep

1. **Review** the Azure Government limitation document
2. **Prepare** your use case summary (E911 IVR with Avaya integration)
3. **Identify** your timeline constraints
4. **Determine** acceptable alternatives (if any)
5. **Bring** the questions list above
6. **Document** the PM's responses for stakeholder reporting

---

Good luck with your meeting! The documentation provides a solid foundation for the discussion, and you have clear evidence of the limitations to show the PM.

**Contact info saved:** Meeting with Azure ACS PM this week regarding Azure Government Direct Routing limitations.
