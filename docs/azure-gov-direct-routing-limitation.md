# Azure Communication Services Direct Routing in Azure Government

## Status Update

**Last Updated:** May 27, 2026  
**Status:** ✅ **NOW AVAILABLE** - Direct Routing is supported in Azure Government  
**Confirmed By:** Azure Communication Services Program Engineer  
**Environment:** Azure US Government Cloud  
**Subscription:** 78003893-6e88-4fa2-a8f0-315067f22e79  
**ACS Resource:** ivr-dev-acs-bld64pwxb4ukq

---

## Historical Context (May 19, 2026)

When initially attempting to configure custom domains for Direct Routing in Azure Communication Services on Azure Government Cloud, the following error occurred:

```
Response status code does not indicate success: 404 (Not Found).
{
  "error": {
    "code": "ResourceTypeRegistrationNotFound",
    "message": "The resource type registration 'Microsoft.Communication/communicationServices/domains' could not be found."
  }
}
```

This was initially believed to be a feature availability limitation in Azure Government Cloud.

## Resolution (May 27, 2026)

**Confirmed with Azure Communication Services Program Engineer:** Direct Routing IS supported in Azure Government Cloud. The feature is available and working.

## Feature Availability Matrix

| Feature | Commercial Azure | Azure Government |
|---------|------------------|------------------|
| Azure Communication Services (basic) | ✅ GA | ✅ GA |
| Call Automation SDK | ✅ GA | ✅ GA (confirmed working) |
| Event Grid Integration | ✅ GA | ✅ GA (confirmed working) |
| **Direct Routing** | ✅ GA | ✅ **NOW AVAILABLE** |
| **Custom Domains** | ✅ GA | ✅ **NOW AVAILABLE** |
| Phone Numbers (ACS-native) | ✅ GA | ⚠️ Limited availability |

---

## Next Steps

To configure Direct Routing for your ACS resource in Azure Government:

1. **Follow the ACS Configuration Guide**  
   Complete documentation: [`docs/acs-configuration-guide.md`](acs-configuration-guide.md)

2. **Use the Automated Configuration Script**  
   ```powershell
   # From the scripts directory
   cd scripts
   .\Run-AcsConfiguration.ps1
   ```

3. **Prerequisites for Direct Routing**
   - ✅ A domain name you own with DNS management access
   - ✅ Ability to add TXT records to your DNS
   - ✅ Session Border Controller (SBC) with TLS certificate
   - ✅ Avaya Call Manager or other telephony system

---

## Historical Alternative Approaches

### Option 1: Use Native ACS Phone Numbers (For testing without Direct Routing)

If Direct Routing isn't required yet, you can use phone numbers purchased directly through ACS:

```powershell
# List available phone numbers in your area code
az communication phonenumber list --resource-group "rg-ivr-dev" --communication-service "ivr-dev-acs-bld64pwxb4ukq"

# Search for available numbers
az communication phonenumber search `
    --resource-group "rg-ivr-dev" `
    --communication-service "ivr-dev-acs-bld64pwxb4ukq" `
    --area-code "555" `
    --phone-number-type "tollFree" # or "local"

# Purchase a phone number
az communication phonenumber purchase `
    --resource-group "rg-ivr-dev" `
    --communication-service "ivr-dev-acs-bld64pwxb4ukq" `
    --phone-number "+1555XXXXXXX"
```

**Pros:**
- ✅ No custom domain required
- ✅ No SBC configuration needed
- ✅ Works in Azure Government
- ✅ Faster setup for testing/development

**Cons:**
- ❌ Can't use existing Avaya-assigned numbers directly
- ❌ May have limited number availability in Government cloud
- ❌ Requires call forwarding from Avaya to ACS numbers

### Option 2: Wait for Azure Government Feature Parity

Check with your Microsoft account team about the roadmap for Direct Routing in Azure Government.

**Actions:**
1. Open a support ticket with Microsoft to inquire about Direct Routing availability
2. Request to be notified when the feature becomes available
3. Ask if there's a private preview program you can join

**Support Ticket Template:**
```
Subject: Direct Routing Custom Domain Support in Azure Government

We are developing an E911 IVR system on Azure Government Cloud using Azure Communication Services.
We need to configure Direct Routing to integrate with our existing Avaya Communication Manager,
but the custom domain configuration APIs return "ResourceTypeRegistrationNotFound" errors.

Environment:
- Cloud: Azure US Government
- Subscription: 78003893-6e88-4fa2-a8f0-315067f22e79
- ACS Resource: ivr-dev-acs-bld64pwxb4ukq
- Required Feature: Direct Routing with custom domains (Microsoft.Communication/communicationServices/domains)

Questions:
1. Is Direct Routing with custom domains available in Azure Government?
2. If not, what is the expected GA date?
3. Are there any preview programs or workarounds available?
4. What alternative architectures do you recommend for Avaya/ACS integration in Government cloud?
```

### Option 3: Hybrid Architecture (Recommended for Production)

Use a hybrid approach where calls flow through your existing infrastructure first:

```
Avaya Call Manager → SIP Trunk → Third-Party SIP Provider → ACS (via API/SDK)
                                  (Twilio, Bandwidth, etc.)
```

**Implementation Steps:**

1. **Configure Avaya to Route to SIP Trunking Provider**
   - Set up SIP trunk from Avaya CM to provider (Twilio, Bandwidth)
   - No custom domain required on ACS side

2. **Use SIP Provider's Programmable Voice**
   - Twilio: TwiML to forward calls to Azure Functions HTTP endpoint
   - Bandwidth: XML to invoke your IVR HTTP API
   
3. **Handle Calls via REST API**
   - Modify your IVR Functions to accept HTTP webhooks from SIP provider
   - Use Call Automation SDK to manage call state
   - Play audio via provider's APIs or ACS depending on where call originated

4. **Update IVR Code**
   - Add HTTP trigger for SIP provider webhooks
   - Map provider's call events to your existing call flow engine
   - Use provider's TTS/STT capabilities or Azure Cognitive Services

**Example for Twilio Integration:**

```csharp
[Function("TwilioIncomingCall")]
public async Task<IActionResult> TwilioWebhook(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
{
    // Parse Twilio webhook
    var form = await req.ReadFormAsync();
    var callSid = form["CallSid"];
    var from = form["From"];
    var to = form["To"];
    
    // ANI/ALI lookup (existing code)
    var callContext = await _callFlowEngine.BuildCallContextAsync(from, to);
    
    // Return TwiML to handle call
    var response = new VoiceResponse();
    response.Say("Welcome to the IVR system");
    response.Gather(numDigits: 1, action: $"{_baseUrl}/api/twilio/gather/{callContext.CallId}");
    
    return new ContentResult
    {
        Content = response.ToString(),
        ContentType = "text/xml"
    };
}
```

### Option 4: Use Azure Portal (Worth Trying)

Even though the API doesn't work, try the Azure Portal:

1. Navigate to **Azure Portal (https://portal.azure.us)**
2. Go to your ACS resource: **ivr-dev-acs-bld64pwxb4ukq**
3. Check if **"Domains"** or **"Direct Routing"** sections exist in the left menu
4. If they don't appear, the feature isn't available

## Verification Needed

Check Azure Government feature documentation:

```powershell
# Check what operations are available for Communication Services in your cloud
az provider show --namespace Microsoft.Communication --query "resourceTypes[?resourceType=='communicationServices'].operations[].name" --output table
```

## Recommended Next Steps

**Immediate (for development/testing):**
1. ✅ Use native ACS phone numbers for initial testing
2. ✅ Test Event Grid → IncomingCallHandler flow with ACS numbers
3. ✅ Validate call automation, TTS, STT, and AI integration
4. ✅ Complete all IVR logic development and testing

**Short-term (prepare for production):**
1. 📞 Contact Microsoft support for Direct Routing roadmap
2. 🔍 Evaluate third-party SIP trunking providers (Twilio, Bandwidth)
3. 📝 Document architecture trade-offs for stakeholders
4. 🧪 Prototype hybrid SIP provider → Azure Functions approach

**Long-term (production deployment):**
1. ⏳ Wait for Azure Government feature parity OR
2. 🚀 Deploy hybrid architecture with SIP trunking provider OR
3. 🔄 Consider commercial Azure if Direct Routing is critical (security review required)

## Documentation Updates Needed

The following documentation should be updated to note Azure Government limitations:

- ✏️ [docs/acs-configuration-guide.md](./acs-configuration-guide.md) - Add Azure Government notice
- ✏️ [docs/architecture.md](./architecture.md) - Document alternative architectures
- ✏️ [docs/azure-gov-deployment.md](./azure-gov-deployment.md) - Add ACS Direct Routing limitations

## References

- [Azure Services by Region](https://azure.microsoft.com/global-infrastructure/services/)
- [Azure Government Documentation](https://docs.microsoft.com/azure/azure-government/)
- [ACS Direct Routing Documentation](https://learn.microsoft.com/azure/communication-services/concepts/telephony-sms/direct-routing-infrastructure)
- [Azure Government Services Parity](https://docs.microsoft.com/azure/azure-government/compare-azure-government-global-azure)

---

**Status:** Issue documented, alternatives provided  
**Blocking:** Direct Routing configuration  
**Workaround:** Use native ACS phone numbers for testing, evaluate hybrid architecture for production  
**Next Action:** Contact Microsoft support & stakeholder decision on architecture approach
