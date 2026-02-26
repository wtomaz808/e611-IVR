# System Integration Guide

This document covers every integration point in the IVR system: AI-powered transcript routing, external system connectivity, webhook dispatching, and the data extraction pipeline.

---

## Table of Contents

1. [AI-Powered Team Routing](#1-ai-powered-team-routing)
2. [External System Integration](#2-external-system-integration)
3. [Webhook Actions](#3-webhook-actions)
4. [ANI/ALI Data Integration](#4-aniali-data-integration)
5. [PSTN Connectivity](#5-pstn-connectivity)
6. [Azure Communication Services](#6-azure-communication-services)
7. [Azure OpenAI Integration](#7-azure-openai-integration)
8. [Configuration Reference](#8-configuration-reference)

---

## 1. AI-Powered Team Routing

### Overview

When a menu has `EnableSpeechRecognition = true`, the IVR captures the caller's spoken words and sends them to Azure OpenAI (GPT-4o) for intent classification. The AI matches the transcript against configured teams and returns a confidence score.

### How It Works

```
Caller speaks → Azure Cognitive Services (STT) → Transcript
    → TranscriptRoutingService → Azure OpenAI (GPT-4o)
        → { intent: "Billing", confidence: 0.92 }
            → Transfer to Billing team's phone number
```

### TeamRoutingConfig Model

Each team is defined as a `TeamRoutingConfig` document in the `TeamRouting` Cosmos DB container:

```json
{
  "id": "team-billing-001",
  "teamName": "Billing",
  "description": "Handles billing inquiries, payment issues, account charges",
  "transferNumber": "+15551234567",
  "queueName": "billing-queue",
  "intentKeywords": [
    "bill", "payment", "charge", "invoice", "refund", "account balance"
  ],
  "confirmationPromptId": "prompt-billing-confirm",
  "priority": 10,
  "isActive": true,
  "partitionKey": "team-routing"
}
```

| Field | Type | Description |
|---|---|---|
| `teamName` | string | Human-readable name, also used as the AI intent label |
| `description` | string | Fed to the AI prompt to improve classification accuracy |
| `transferNumber` | string | E.164 phone number or SIP URI for call transfer |
| `queueName` | string? | Optional queue name for queue-based routing |
| `intentKeywords` | string[] | Example phrases that anchor the AI's classification |
| `confirmationPromptId` | string? | Prompt played before transfer (e.g., "I'll connect you with Billing") |
| `priority` | int | Tiebreaker when multiple teams could match |

### Confidence Thresholds

| Threshold | Value | Behavior |
|---|---|---|
| High confidence | ≥ 0.80 | Route immediately to the matched team |
| Minimum match | ≥ 0.40 | Route with confirmation prompt (if configured) |
| Below minimum | < 0.40 | Trigger the menu's `SpeechFallbackAction` |

### Menu Configuration for Speech Routing

```json
{
  "id": "menu-main",
  "name": "Main Menu",
  "menuType": "SpeechRouting",
  "enableSpeechRecognition": true,
  "speechRoutingPromptId": "prompt-describe-issue",
  "teamRoutingConfigIds": [
    "team-billing-001",
    "team-tech-support-002",
    "team-sales-003"
  ],
  "speechFallbackAction": {
    "type": "TransferToNumber",
    "transferNumber": "+15559999999"
  }
}
```

### AI Prompt Structure

The `TranscriptRoutingService` builds a system prompt containing all team definitions and sends it to GPT-4o with `temperature: 0.1` and JSON response format:

```
System: You are an IVR call routing assistant...
  Available teams:
  - "Billing" Description: Handles billing inquiries... Keywords: bill, payment...
  - "Technical Support" Description: ...

User: Caller said: "I need help with my last month's bill, there's a charge I don't recognize"

AI Response:
{
  "intent": "Billing",
  "confidence": 0.92,
  "reasoning": "Caller mentioned bill and unrecognized charge"
}
```

---

## 2. External System Integration

### Overview

The external system integration pipeline allows the IVR to extract structured data from a caller's transcript using AI and submit it to external REST APIs (fire alarm panels, CAD systems, work order platforms, etc.).

### End-to-End Flow

```
Caller says: "Put the fire alarms in Building 7 into test mode"
    │
    ▼
┌─────────────────────────────────┐
│ 1. Speech Recognition (STT)     │
│    Transcript captured           │
└─────────────┬───────────────────┘
              ▼
┌─────────────────────────────────┐
│ 2. AI Data Extraction            │
│    Azure OpenAI extracts:        │
│    {                             │
│      action: "test",             │
│      location: "Building 7",    │
│      systemType: "fire_alarm"   │
│    }                             │
└─────────────┬───────────────────┘
              ▼
┌─────────────────────────────────┐
│ 3. Template Substitution         │
│    URL:  /alarms/{{location}}/   │
│          test-mode               │
│    Body: { "action": "{{action}}",│
│            "building": "{{loc}}" }│
└─────────────┬───────────────────┘
              ▼
┌─────────────────────────────────┐
│ 4. HTTP Dispatch                 │
│    POST https://firealarm.api/   │
│         alarms/Building%207/     │
│         test-mode                │
│    Auth: API Key header          │
│    Retry: 3x with backoff       │
└─────────────┬───────────────────┘
              ▼
┌─────────────────────────────────┐
│ 5. Confirmation                  │
│    Extract ticketId from response│
│    Play: "Done. Reference number │
│           FA-2024-1234."         │
└─────────────────────────────────┘
```

### Configuration Components

Three linked configs drive the pipeline:

#### A. ExternalSystemConfig

Defines the external API connection (stored in `ExternalSystems` container):

```json
{
  "id": "sys-fire-alarm-001",
  "systemName": "Fire Alarm Panel",
  "description": "Building fire alarm control system",
  "systemType": "RestApi",
  "baseUrl": "https://firealarm.example.com/api/v1",
  "endpoints": [
    {
      "actionName": "put-in-test",
      "description": "Put fire alarm into test mode",
      "httpMethod": "POST",
      "urlPath": "/alarms/{{location}}/test-mode",
      "payloadTemplate": "{ \"action\": \"{{action}}\", \"building\": \"{{location}}\", \"requestedBy\": \"{{callerNumber}}\" }",
      "responseConfirmationField": "$.ticketId",
      "confirmationMessageTemplate": "Done. Fire alarms at {{location}} have been placed in {{action}} mode. Your reference number is {{confirmationValue}}."
    },
    {
      "actionName": "take-out-of-test",
      "httpMethod": "POST",
      "urlPath": "/alarms/{{location}}/normal-mode",
      "payloadTemplate": "{ \"building\": \"{{location}}\", \"requestedBy\": \"{{callerNumber}}\" }",
      "responseConfirmationField": "$.ticketId"
    }
  ],
  "authType": "ApiKey",
  "authConfig": {
    "apiKeyHeaderName": "X-API-Key",
    "apiKeyValue": "your-api-key-here"
  },
  "headers": {
    "X-Source": "IVR-System"
  },
  "retryPolicy": {
    "maxRetries": 3,
    "initialDelayMs": 1000,
    "backoffMultiplier": 2.0
  },
  "timeoutSeconds": 30,
  "isActive": true,
  "partitionKey": "external-system"
}
```

#### B. DataExtractionConfig

Defines which fields the AI should extract from the transcript (stored in `DataExtraction` container):

```json
{
  "id": "extract-fire-alarm-001",
  "name": "Fire Alarm Actions",
  "description": "Extract fire alarm action details from caller transcript",
  "fields": [
    {
      "fieldName": "action",
      "label": "Action",
      "description": "The action the caller wants to perform on the fire alarm system",
      "dataType": "Enum",
      "required": true,
      "validValues": ["test", "reset", "silence", "acknowledge"],
      "valueAliases": {
        "put in test": "test",
        "testing": "test",
        "quiet": "silence",
        "shut off": "silence"
      }
    },
    {
      "fieldName": "location",
      "label": "Location",
      "description": "The building or floor where the fire alarm is located",
      "dataType": "String",
      "required": true
    },
    {
      "fieldName": "systemType",
      "label": "System Type",
      "description": "Type of alarm system",
      "dataType": "Enum",
      "required": false,
      "defaultValue": "fire_alarm",
      "validValues": ["fire_alarm", "sprinkler", "smoke_detector"]
    }
  ],
  "externalSystemId": "sys-fire-alarm-001",
  "endpointActionName": "put-in-test",
  "aiContextInstructions": "The caller is reporting a fire alarm system action. Locations are typically building numbers or floor names within a campus.",
  "requireCallerConfirmation": false,
  "successTtsTemplate": "Done. Fire alarms at {{location}} have been placed in {{action}} mode. Your reference number is {{confirmationValue}}.",
  "failurePromptId": "prompt-system-error",
  "postSubmitAction": {
    "type": "Hangup",
    "promptId": "prompt-goodbye"
  },
  "isActive": true,
  "partitionKey": "data-extraction"
}
```

#### C. Menu Action (links into the call flow)

```json
{
  "dtmfKey": "3",
  "label": "Fire Alarm Services",
  "action": {
    "type": "SubmitToExternalSystem",
    "dataExtractionConfigId": "extract-fire-alarm-001"
  }
}
```

### Authentication Methods

The system supports three authentication methods for external APIs:

| Auth Type | Configuration | HTTP Header |
|---|---|---|
| **API Key** | `apiKeyHeaderName` + `apiKeyValue` | Custom header (e.g., `X-API-Key: abc123`) |
| **Bearer Token** | `bearerToken` | `Authorization: Bearer <token>` |
| **Basic Auth** | `username` + `password` | `Authorization: Basic <base64(user:pass)>` |

Example configurations:

```json
// API Key
{
  "authType": "ApiKey",
  "authConfig": {
    "apiKeyHeaderName": "X-API-Key",
    "apiKeyValue": "your-key"
  }
}

// Bearer Token
{
  "authType": "BearerToken",
  "authConfig": {
    "bearerToken": "eyJhbGciOi..."
  }
}

// Basic Auth
{
  "authType": "BasicAuth",
  "authConfig": {
    "username": "ivr-service",
    "password": "secure-password"
  }
}
```

> **Security Note**: In production, store credentials in Azure Key Vault and reference them via Key Vault references in App Settings, rather than storing them directly in Cosmos DB documents.

### Retry Policy

Failed HTTP requests are retried with exponential backoff:

| Attempt | Delay | Condition |
|---|---|---|
| 1st try | 0 ms | — |
| 2nd try | 1000 ms | Only on 5xx, 408 (timeout), or 429 (rate limit) |
| 3rd try | 2000 ms | Only on 5xx, 408, or 429 |
| 4th try | 4000 ms | Only on 5xx, 408, or 429 |

Client errors (4xx except 408/429) are **not retried** — they indicate a configuration problem.

### Response Handling

The `responseConfirmationField` uses a dot-path expression to extract a value from the external system's JSON response:

```json
// External API response:
{
  "status": "success",
  "data": {
    "ticketId": "FA-2024-1234",
    "estimatedTime": "5 minutes"
  }
}

// responseConfirmationField: "data.ticketId"
// Extracted confirmationValue: "FA-2024-1234"
```

The `{{confirmationValue}}` placeholder in TTS templates is replaced with this extracted value.

### Template Placeholders

Both URL paths and payload templates support `{{fieldName}}` placeholders that are replaced with extracted data at runtime:

| Placeholder | Source |
|---|---|
| `{{action}}`, `{{location}}`, etc. | AI-extracted fields |
| `{{callerNumber}}` | Auto-injected from call context |
| `{{timestamp}}` | Auto-injected (ISO 8601 UTC) |
| `{{confirmationValue}}` | Extracted from external system response (TTS templates only) |
| Any key from `customData` | Injected from the menu action's `customData` dictionary |

---

## 3. Webhook Actions

### Overview

Webhook actions fire a simple HTTP POST to a configured URL with call context data. Unlike `SubmitToExternalSystem` (which uses AI extraction), webhooks send raw call data as-is.

### Menu Configuration

```json
{
  "dtmfKey": "5",
  "label": "Send Notification",
  "action": {
    "type": "Webhook",
    "webhookUrl": "https://hooks.example.com/ivr-events",
    "customData": {
      "priority": "high",
      "department": "facilities"
    }
  }
}
```

### Webhook Payload

The system sends a JSON POST with this structure:

```json
{
  "callId": "abc-123-def",
  "callerNumber": "+15551234567",
  "calledNumber": "+15559876543",
  "transcript": "I need someone to check the sprinkler system",
  "detectedIntent": "Maintenance",
  "routedToTeam": "Facilities",
  "menuPath": [
    { "menuId": "menu-main", "menuName": "Main Menu", "input": "3" },
    { "menuId": "menu-facilities", "menuName": "Facilities", "input": "5" }
  ],
  "metadata": {},
  "customData": {
    "priority": "high",
    "department": "facilities"
  },
  "timestamp": "2026-02-17T14:30:00Z"
}
```

Webhook calls are fire-and-forget — failures are logged but do not affect the call flow.

---

## 4. ANI/ALI Data Integration

### ANI (Automatic Number Identification)

Associates a phone number with caller metadata for personalized routing:

```json
{
  "phoneNumber": "+15551234567",
  "callerName": "John Smith",
  "accountNumber": "ACCT-001234",
  "callerType": "Business",
  "priority": 5,
  "language": "en-US",
  "vipFlag": true,
  "blockedFlag": false,
  "customRouting": "menu-vip-main",
  "metadata": {
    "region": "northeast",
    "contractLevel": "premium"
  }
}
```

### ALI (Automatic Location Identification)

Associates a phone number with a physical location:

```json
{
  "phoneNumber": "+15551234567",
  "address": {
    "street": "123 Main St",
    "city": "New York",
    "state": "NY",
    "zipCode": "10001",
    "country": "US"
  },
  "coordinates": {
    "latitude": 40.7484,
    "longitude": -73.9967
  },
  "locationType": "Commercial",
  "serviceArea": "Manhattan",
  "timezone": "America/New_York",
  "region": "northeast"
}
```

### Routing Impact

The `CallFlowEngine` checks ANI/ALI data in this order:

1. **Blocked** (`IsBlocked = true`) → Call rejected
2. **VIP** (`IsVip = true` + `CustomRoutingMenuId` set) → Custom menu tree
3. **CallerType condition** → Menu condition matching
4. **Region/ServiceArea condition** → Location-based routing
5. **Language** → Passed to TTS voice selection

### Phone Number Normalization

All phone numbers are normalized to **E.164 format** before lookup. The `AniAliService` also handles SIP URIs and other identifiers from direct routing:

| Input | Normalized |
|---|---|
| `5551234567` | `+15551234567` |
| `15551234567` | `+15551234567` |
| `(555) 123-4567` | `+15551234567` |
| `+15551234567` | `+15551234567` (unchanged) |
| `sip:+15551234567@sbc.contoso.com` | `+15551234567` |
| `tel:+15551234567` | `+15551234567` |
| `4:+15551234567` | `+15551234567` (ACS rawId) |

---

## 5. PSTN Connectivity

### Overview

The system supports three PSTN connection modes, configured via `SystemConfig.PstnMode`:

| Mode | Description | Use Case |
|---|---|---|
| **NativeAcs** | Phone numbers purchased inside Azure Communication Services | New deployments with no existing telephony infrastructure |
| **DirectRouting** | Existing PSTN numbers routed through a customer-owned Session Border Controller (SBC) into ACS | Organizations with existing PBX/SBC infrastructure and carrier contracts |
| **Hybrid** | Mix of ACS-native and direct-routed numbers on the same IVR deployment | Phased migration or multi-site deployments |

### Architecture — Direct Routing

```
  Caller
    │
    ▼
┌──────────┐    SIP INVITE    ┌──────────────────┐   REST/WS    ┌──────────────┐
│  PSTN    │ ─────────────►  │  Session Border   │ ──────────► │ Azure Comm.  │
│  Carrier │                  │  Controller (SBC) │              │ Services     │
└──────────┘                  └──────────────────┘              └──────┬───────┘
                                                                       │
                                                            Event Grid │
                                                                       ▼
                                                             ┌──────────────────┐
                                                             │ IVR Functions    │
                                                             │ (IncomingCall    │
                                                             │  Handler)        │
                                                             └──────────────────┘
```

### PhoneNumberConfig Model

Each phone number (DID) in the system has a `PhoneNumberConfig` document stored in the `PhoneNumbers` Cosmos DB container:

```json
{
  "id": "pn-001",
  "phoneNumber": "+15551234567",
  "label": "Main Support Line",
  "numberType": "DirectRouting",
  "rootMenuId": "menu-support-root",
  "businessHoursConfigId": "bh-support",
  "welcomePromptId": "prompt-welcome-support",
  "sbcFqdn": "sbc.contoso.com",
  "sbcPort": 5067,
  "calledNumberAliases": ["+15551234568"],
  "isActive": true,
  "partitionKey": "phone-number"
}
```

| Field | Type | Description |
|---|---|---|
| `phoneNumber` | string | E.164 phone number |
| `label` | string | Human-readable name (e.g., "Main Line") |
| `numberType` | enum | `NativeAcs`, `DirectRouting`, or `SipTrunk` |
| `rootMenuId` | string? | Per-DID root menu (overrides system-wide root) |
| `businessHoursConfigId` | string? | Per-DID business hours override |
| `welcomePromptId` | string? | Welcome prompt before root menu |
| `sbcFqdn` | string? | SBC FQDN (required for direct routing) |
| `sbcPort` | int | SIP signaling port (default: 5067) |
| `calledNumberAliases` | string[] | Alternate called-number identities |
| `isActive` | bool | Whether the number is active |

### Per-DID Menu Routing

When a call arrives, the `CallFlowEngine` checks the `PhoneNumbers` collection **before** the normal priority waterfall:

1. Normalize the called number (including SIP URI parsing)
2. Look up `PhoneNumberConfig` by `phoneNumber` or `calledNumberAliases`
3. If found and active with a `rootMenuId` → use that menu as the entry point
4. If not found → fall through to the system-wide root menu

This allows multiple phone numbers to share one IVR deployment, each with a different menu tree, prompts, and business hours.

### SIP URI Handling

For direct-routed calls, ACS provides caller/called identifiers as SIP URIs rather than plain phone numbers. The `IncomingCallHandler` extracts the phone number using a two-step fallback:

1. Try `participant.phoneNumber.value` (ACS-native format)
2. Fall back to `participant.rawId` and parse with `AniAliService.ExtractPhoneFromUri()`

Supported URI formats:

| URI Format | Extracted Number |
|---|---|
| `sip:+15551234567@sbc.contoso.com` | `+15551234567` |
| `sip:15551234567@sbc.contoso.com` | `+15551234567` |
| `tel:+15551234567` | `+15551234567` |
| `4:+15551234567` | `+15551234567` |

### SystemConfig PSTN Settings

The `SystemConfig` document includes system-wide PSTN defaults:

| Field | Type | Default | Description |
|---|---|---|---|
| `pstnMode` | `PstnMode` enum | `NativeAcs` | `NativeAcs`, `DirectRouting`, or `Hybrid` |
| `defaultSbcFqdn` | string? | `null` | Default SBC FQDN (overridden per-number) |
| `defaultSbcPort` | int | `5067` | Default SIP signaling port |
| `enableDisconnectTransferToVdn` | bool | `false` | Forward caller disconnects to the CM10 VDN |
| `defaultCm10Vdn` | string? | `null` | SIP URI or E.164 of the default Avaya CM10 VDN |
| `cm10SbcFqdn` | string? | `null` | SBC FQDN for VDN transfers (falls back to defaultSbcFqdn) |
| `cm10SbcPort` | int? | `null` | SBC SIP port for VDN transfers (falls back to defaultSbcPort) |
| `cm10TransferPromptId` | string? | `null` | Prompt played before a VDN transfer |

### Setting Up Direct Routing

1. **Provision an SBC** — deploy a certified SBC (e.g., AudioCodes, Ribbon) and configure it to route SIP trunks to ACS.
2. **Register the SBC in ACS** — use the Azure portal or CLI to add the SBC FQDN as a direct routing trunk.
3. **Configure voice routes** — create voice routing policies in ACS matching the phone number patterns you want to receive.
4. **Create PhoneNumberConfig** — add a document in Cosmos DB for each DID, specifying `numberType: "DirectRouting"` and the SBC FQDN.
5. **Set PstnMode** — update `SystemConfig.PstnMode` to `DirectRouting` or `Hybrid`.
6. **Test** — place a call to the DID. The `IncomingCallHandler` will parse the SIP URI, look up the `PhoneNumberConfig`, and route to the correct menu.

### Avaya CM10 VDN Integration

The IVR supports transferring calls to **Avaya Communication Manager 10 (CM10)** Vector Directory Numbers (VDNs) so the Avaya ACD can handle agent queuing after the IVR self-service flow completes.

#### How It Works

```
PSTN → CM10 (PRI/SIP) → SBC → Azure ACS (Direct Routing)
                                      ↓
                                IVR Functions
                                (self-service)
                                      ↓
                        ┌─────────────────────────────┐
                        │  Caller hangs up / IVR done  │
                        └──────────────┬──────────────┘
                                       ↓
                              TransferToVdn action
                                       ↓
                           SIP INVITE → SBC → CM10
                                       ↓
                              Avaya ACD Queue → Agent
```

#### Disconnect-to-VDN Transfer

When `enableDisconnectTransferToVdn` is `true` in `SystemConfig`, the `CallbackHandler` intercepts the `CallDisconnected` event and, instead of finalizing the call as a hangup, transfers it to the configured `defaultCm10Vdn`. This ensures every call that passes through the IVR is handed off to the CM10 ACD for agent handling.

Conditions:
- The call must not have already been transferred (avoids double-transfer).
- The `defaultCm10Vdn` must be set to a valid SIP URI or E.164 number.
- If the transfer fails, the call is finalized as a normal disconnect.

#### TransferToVdn Menu Action

You can also explicitly transfer calls to a VDN as part of the IVR call flow using the `TransferToVdn` action type on any menu option:

```json
{
  "dtmfKey": "0",
  "label": "Speak to an Agent",
  "action": {
    "type": "TransferToVdn",
    "vdnAddress": "sip:70100@sbc.contoso.com",
    "promptId": "transfer-hold-prompt"
  }
}
```

| Field | Description |
|---|---|
| `vdnAddress` | SIP URI, extension, or E.164 of the CM10 VDN. Falls back to `SystemConfig.DefaultCm10Vdn` if null. |
| `promptId` | Optional prompt played before the transfer (e.g., "Please hold while we connect you.") |

#### VDN Address Resolution

The system resolves the target VDN in this order:

1. **SIP URI** (`sip:70100@sbc.contoso.com`) — used as-is for a direct SIP transfer.
2. **Extension / VDN number** (`70100`) — combined with `cm10SbcFqdn` (or `defaultSbcFqdn`) to form `sip:70100@sbc.contoso.com`.
3. **E.164 number** (`+18005550100`) — transferred via PSTN using `PhoneNumberIdentifier`.

#### Setting Up CM10 Integration

1. **Configure SBC routing** — ensure the Avaya SBC routes SIP INVITEs from ACS to the CM10.
2. **Create VDNs in CM10** — set up the VDNs and call vectors you want the IVR to transfer to.
3. **Set SystemConfig** — in the Admin Portal under **Settings > Avaya CM10 Integration**:
   - Enable **Transfer to VDN on Disconnect**.
   - Enter the **Default CM10 VDN** (e.g., `sip:70100@sbc.contoso.com`).
   - Enter the **CM10 SBC FQDN** (if different from the main SBC).
   - Optionally set a **Transfer Prompt** to play before the handoff.
4. **Test** — place a call, navigate the IVR, and hang up. The call should transfer to the CM10 VDN instead of ending.

---

## 6. Azure Communication Services

### Call Control Operations

The IVR uses the ACS **Call Automation SDK** for:

| Operation | SDK Method | Usage |
|---|---|---|
| Answer call | `AnswerCallAsync` | IncomingCallHandler — answers with callback URL |
| Reject call | `RejectCallAsync` | IncomingCallHandler — rejects blocked callers |
| Play audio | `PlayToAllAsync` | Plays TTS, audio files, or SSML prompts |
| Collect DTMF | `StartRecognizingAsync` (DTMF) | Standard menus — collects touch-tone digits |
| Collect speech | `StartRecognizingAsync` (Speech) | Speech menus — captures spoken words |
| Transfer call | `TransferCallToParticipantAsync` | Routes to external numbers, teams, or CM10 VDNs |
| Hang up | `HangUpAsync` | Terminates the call |

### Callback Flow

ACS sends call events to the callback URL registered during `AnswerCallAsync`:

```
POST /api/callbacks/{callId}
Content-Type: application/cloudevents+json

[CloudEvent with CallConnected | RecognizeCompleted | PlayCompleted | etc.]
```

The `CallbackHandler` function parses these CloudEvents and dispatches to the appropriate handler.

---

## 7. Azure OpenAI Integration

The system uses Azure OpenAI in two distinct pipelines:

### Intent Classification (TranscriptRoutingService)

- **Model**: GPT-4o
- **Temperature**: 0.1 (deterministic)
- **Response format**: JSON object
- **Max tokens**: 200
- **Purpose**: Classify caller speech into one of N configured teams

### Data Extraction (ExternalSystemIntegrationService)

- **Model**: GPT-4o
- **Temperature**: 0.1 (deterministic)
- **Response format**: JSON object
- **Max tokens**: 500
- **Purpose**: Extract structured fields from caller speech for external system submission

Both services support API key or managed identity authentication:

```csharp
// API Key (local dev / non-production)
new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));

// Managed Identity (production)
new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
```

---

## 8. Configuration Reference

### Environment Variables (local.settings.json / App Settings)

| Variable | Required | Description |
|---|---|---|
| `AcsConnectionString` | Yes | Azure Communication Services connection string |
| `CosmosDbConnectionString` | Yes | Cosmos DB connection string |
| `StorageConnectionString` | Yes | Azure Blob Storage connection string |
| `CallbackBaseUrl` | Yes | Public URL of the Function App (e.g., `https://ivr-func.azurewebsites.net`) |
| `CognitiveServicesEndpoint` | Yes | Azure Cognitive Services Speech endpoint URL |
| `CognitiveServicesKey` | No | Cognitive Services key (uses managed identity if absent) |
| `AzureOpenAI:Endpoint` | Yes | Azure OpenAI endpoint URL |
| `AzureOpenAI:ApiKey` | No | Azure OpenAI API key (uses managed identity if absent) |
| `AzureOpenAI:DeploymentName` | No | GPT model deployment name (default: `gpt-4o`) |

### Adding a New External System Integration

1. **Create an `ExternalSystemConfig`** in Cosmos DB defining the API base URL, endpoints, auth, and retry policy.
2. **Create a `DataExtractionConfig`** defining the fields to extract, linking to the external system ID and endpoint action name.
3. **Add a menu option** with `ActionType.SubmitToExternalSystem` and the `DataExtractionConfigId`.
4. **Test** by calling the IVR and speaking a request that matches the extraction fields.

### Adding a New Team for Speech Routing

1. **Create a `TeamRoutingConfig`** in Cosmos DB with the team name, description, keywords, and transfer number.
2. **Add the config ID** to the speech-routing menu's `teamRoutingConfigIds` array.
3. **Test** by calling the IVR on a speech-enabled menu and describing an issue that matches the team.

---

## Testing with the PSTN Simulator

For end-to-end testing of CM10 VDN transfers, team routing, and external system integrations without real telephony hardware, use the [PSTN & CM10 Simulator](pstn-simulator.md).

The simulator provides:

- **Mock ACS mode** — Fake ACS REST API, no Azure required
- **CM10 engine** — Simulated VDNs, vectors, ACD queues, and agents
- **DTMF/Speech input** — Test all recognize types from the browser
- **Transfer tracking** — Watch calls flow from IVR → CM10 → Agent

See also: [CM10 Setup Guide](cm10-setup-guide.md) for physical Avaya CM10 configuration.
