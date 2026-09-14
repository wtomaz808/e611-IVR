# Teams Integration Field Guide

**Audience:** Field engineers, deployment teams  
**Purpose:** Step-by-step guide for connecting a customer's Microsoft Teams phone number to the e611-IVR system
**Assumption:** The IVR Function App and Bot Service are already deployed and running in your Azure Government tenant.

---

## Overview

When a customer is ready to go live, they provide you with a phone number provisioned in their Microsoft Teams Phone System. Your job is to wire that number to the IVR bot endpoint. There are three systems involved and clear ownership boundaries between them:

| System | Owner | What Happens Here |
|---|---|---|
| **Microsoft Teams Phone System** | Customer's M365 tenant admin | Phone number is assigned to a Resource Account linked to your Bot App ID |
| **Bot Framework / App Registration** | You (service provider) | Bot registration holds the messaging endpoint URL and Graph API permissions |
| **Azure Gov (Function App, Cosmos DB, etc.)** | You (service provider) | All IVR logic runs here — no changes needed per customer in most cases |

The critical link is this:

```
Customer's Phone Number
  └─► M365 Resource Account  (linked to your Bot App ID)
        └─► Microsoft Graph  (routes call notifications to your bot endpoint)
              └─► Function App  POST /api/bot-messages
                    └─► Azure resources (Cosmos DB, TTS, OpenAI)
```

End-to-end call flow:

```
Caller dials +17035550911
       │
       ▼
Teams Phone System (customer's M365)
  - Number is assigned to a Resource Account
  - Resource Account is linked to your Bot App ID
       │
       ▼  commsNotification (HTTP POST)
Microsoft Graph Calling API
  - Looks up the bot's registered messaging endpoint
       │
       ▼
Your Function App
  POST https://ivr-teams-func-hgknk444g237w.azurewebsites.us/api/bot-messages
       │
       ▼  Graph API calls back to real Microsoft Graph
  PATCH /communications/calls/{id}  ← answers the call
  POST  /communications/calls/{id}/playPrompt
  POST  /communications/calls/{id}/subscribeToTone
  POST  /communications/calls/{id}/transfer
```

---

## Prerequisites Checklist

Before starting, confirm the following are in place:

**Your side (Azure Gov tenant):**
- [ ] Function App deployed and reachable at `https://<func-app>.azurewebsites.us/api/bot-messages`
- [ ] Bot App Registration created (has `MicrosoftAppId` and `MicrosoftAppPassword`)
- [ ] Bot has the following Microsoft Graph API permissions with **admin consent granted**:
  - `Calls.Initiate.All`
  - `Calls.InitiateGroupCall.All`
  - `Calls.JoinGroupCall.All`
  - `Calls.JoinGroupCallAsGuest.All`
  - `Calls.AccessMedia.All`
- [ ] Teams channel enabled on the Bot Service resource
- [ ] Function App settings configured (see [App Settings](#app-settings) below)

**Customer's side (M365 / Teams tenant):**
- [ ] Teams Phone System licenses available (Teams Phone Resource Account license)
- [ ] Phone number provisioned and ready to assign (Calling Plan, Direct Routing, or Operator Connect)
- [ ] Teams admin has PowerShell access (`MicrosoftTeams` module installed)
- [ ] Your `MicrosoftAppId` shared with the customer's Teams admin

---

## Step 1 — Configure the Bot Messaging Endpoint

In your Azure Bot Service resource (in your Azure Gov tenant):

1. Go to **Azure Portal** → Bot Services → your bot resource
2. Under **Configuration**, set **Messaging endpoint**:
   ```
   https://<your-function-app>.azurewebsites.us/api/bot-messages
   ```
3. Under **Channels**, ensure the **Microsoft Teams** channel is enabled and set to **Microsoft Teams Government**
4. Save

> **Note:** This URL is what Microsoft Graph uses to deliver call notifications. If the Function App URL ever changes (e.g., new deployment), this must be updated.

---

## Step 2 — Customer Teams Admin: Create the Resource Account

The customer's Teams admin runs the following PowerShell in their tenant. Provide them with your `MicrosoftAppId` before this step.

```powershell
# Connect to the customer's Teams tenant
Connect-MicrosoftTeams

# Create a Resource Account linked to your bot's App ID
New-CsOnlineApplicationInstance `
  -UserPrincipalName "ivr-e611@<customer-domain>.gov" `
  -DisplayName "e611-IVR" `
  -ApplicationId "<MicrosoftAppId>"   # ← your bot's Client ID

# Capture the ObjectId from the output — needed in the next step
```

Allow a few minutes for the account to sync across M365, then run:

```powershell
Sync-CsOnlineApplicationInstance -ObjectId "<ObjectId-from-above>"
```

---

## Step 3 — Customer Teams Admin: Assign License and Phone Number

**Assign the license** (in M365 Admin Center or via PowerShell):
- The Resource Account requires a **Microsoft Teams Phone Resource Account** license
- This is a free license — the customer just needs to have it available in their tenant

**Assign the phone number** (PowerShell):

```powershell
# For a Direct Routing number (customer-owned number via SBC):
Set-CsPhoneNumberAssignment `
  -Identity "ivr-e611@<customer-domain>.gov" `
  -PhoneNumber "+1<10-digit-number>" `
  -PhoneNumberType DirectRouting

# For a Microsoft Calling Plan number (Microsoft-provided):
Set-CsPhoneNumberAssignment `
  -Identity "ivr-e611@<customer-domain>.gov" `
  -PhoneNumber "+1<10-digit-number>" `
  -PhoneNumberType CallingPlan

# For an Operator Connect number:
Set-CsPhoneNumberAssignment `
  -Identity "ivr-e611@<customer-domain>.gov" `
  -PhoneNumber "+1<10-digit-number>" `
  -PhoneNumberType OperatorConnect
```

> **Important:** Use the exact E.164 format (`+1XXXXXXXXXX`). The IVR normalizes incoming numbers against its ANI database using this format.

---

## Step 4 — Update Function App Settings

Update the following application settings on the IVR Function App in your Azure Gov tenant. These are set per-customer deployment:

### App Settings

| Setting | Value | Notes |
|---|---|---|
| `MicrosoftAppId` | `<your-bot-app-registration-client-id>` | Same App ID given to the customer's Teams admin |
| `MicrosoftAppPassword` | `<bot-client-secret>` | From your App Registration → Certificates & Secrets |
| `MicrosoftAppTenantId` | `<customer-tenant-id>` | The customer's Azure AD / M365 tenant ID |
| `GraphApiEndpoint` | `https://graph.microsoft.us/v1.0` | Microsoft Graph for Government |
| `ChannelService` | `https://botframework.azure.us` | Bot Framework Government endpoint |
| `CosmosDbConnectionString` | `@Microsoft.KeyVault(SecretUri=https://<vault>.vault.usgovcloudapi.net/secrets/CosmosDbConnectionString/)` | Key Vault reference — rotate the secret in Key Vault, not this setting |
| `StorageConnectionString` | `DefaultEndpointsProtocol=...` | Your Blob Storage (no change) |
| `CognitiveServicesEndpoint` | `https://...cognitiveservices.azure.us/...` | Your Speech service (no change) |
| `CognitiveServicesKey` | `<key>` | Your Speech service key (no change) |

To update via Azure CLI:

```powershell
$rg = "rg-ivr-teams"
$func = "ivr-teams-func-<suffix>"

az functionapp config appsettings set `
  --resource-group $rg `
  --name $func `
  --settings `
    "MicrosoftAppId=<app-id>" `
    "MicrosoftAppPassword=<secret>" `
    "MicrosoftAppTenantId=<customer-tenant-id>" `
    "GraphApiEndpoint=https://graph.microsoft.us/v1.0" `
    "ChannelService=https://botframework.azure.us"
```

---

## Step 5 — Seed the Phone Number in Cosmos DB

The IVR routes calls based on the called DID (phone number). Add the customer's number to the `PhoneNumbers` Cosmos DB container so the IVR knows which menu tree to use.

Using the DataSeeder or directly in Cosmos DB, add a `PhoneNumberConfig` document:

```json
{
  "id": "pn-customer-001",
  "partitionKey": "phone-number",
  "phoneNumber": "+1<10-digit-number>",
  "label": "Customer e611-IVR Main Line",
  "numberType": "TeamsDirectRouting",
  "rootMenuId": "menu-main",
  "isActive": true
}
```

If left empty, all calls will fall through to the system-wide root menu — which may be acceptable for single-number deployments.

---

## Step 6 — Verify End-to-End

Once all steps are complete:

1. **Test call**: Dial the customer's phone number from any PSTN phone
2. **Expected behavior**:
   - Call rings and is answered within 2–3 seconds
   - Welcome prompt plays (TTS audio)
   - DTMF input is accepted (keypad press routes to correct menu)
3. **Verify in Admin Portal**:
   - Call appears in **Call Logs** within seconds of completion
   - Caller number, ANI data, and menu path are recorded correctly
4. **Check Application Insights** for any exceptions if something fails

---

## Multi-Tenant Considerations (Multiple Customers)

If you are deploying for multiple customers on the same Azure infrastructure:

- Each customer has their own M365 tenant and phone number
- Each customer's Teams admin creates a Resource Account pointing to the **same** `MicrosoftAppId`
- Each customer's phone number is added as a separate `PhoneNumberConfig` in Cosmos DB with its own `rootMenuId`
- The Function App setting `MicrosoftAppTenantId` should be set to `common` (multi-tenant) rather than a specific tenant ID, **unless** operating in a single-tenant government environment

> **Government Cloud note:** For Azure US Government (GCC-High / DoD), cross-tenant bot scenarios may be restricted. In most cases each customer deployment will require a dedicated App Registration in or trusted by the customer's tenant. Confirm with the customer's M365 admin before proceeding with a multi-tenant approach.

---

## Troubleshooting

| Symptom | Likely Cause | Resolution |
|---|---|---|
| Call rings but IVR never answers | Messaging endpoint URL wrong in Bot Service | Verify URL in Bot Service → Configuration |
| `401 Unauthorized` in Function App logs | `MicrosoftAppId` or `MicrosoftAppPassword` mismatch | Re-check app settings match the App Registration |
| `403 Forbidden` when calling Graph | Graph API permissions not granted or admin consent missing | Re-run admin consent in the App Registration |
| Call answered but silent (no audio) | TTS not generating — check `CognitiveServicesEndpoint` and key | Review Function App logs for TTS errors |
| Call drops immediately after answer | `MicrosoftAppTenantId` set to wrong tenant | Set to customer tenant ID or `common` |
| Phone number not routing to IVR | Resource Account not synced or license missing | Re-run `Sync-CsOnlineApplicationInstance`; verify license assigned |
| Called number not found in IVR | `PhoneNumberConfig` missing in Cosmos DB | Add the DID to the `PhoneNumbers` container |

---

## Reference

| Item | Value |
|---|---|
| Bot App Registration | Azure Portal → Entra ID → App Registrations |
| Function App endpoint | `https://<func>.azurewebsites.us/api/bot-messages` |
| Microsoft Graph (Gov) | `https://graph.microsoft.us/v1.0` |
| Bot Framework (Gov) | `https://botframework.azure.us` |
| Teams Admin Center | `https://admin.teams.microsoft.us` (Gov) |
| M365 Admin Center | `https://admin.microsoft.us` (Gov) |
| Teams PowerShell module | `Install-Module MicrosoftTeams` |
