# IVR System Architecture

## Overview

The IVR (Interactive Voice Response) system is a cloud-native, multi-level call-handling platform built entirely on Microsoft Azure. It answers inbound phone calls, identifies callers via ANI/ALI data, navigates them through configurable menu trees using DTMF or natural-language speech, and can route calls to teams, trigger webhooks, or push structured data into external systems—all driven by Azure OpenAI.

The system comprises three deployable components:

| Component | Technology | Purpose |
|---|---|---|
| **IVR Engine** | Azure Functions v4 (.NET 8, isolated worker) | Handles inbound calls, menu navigation, speech recognition, AI routing, external system integration |
| **Admin Portal** | Blazor Server (.NET 8) on Azure App Service | Web UI for managing menus, prompts, ANI/ALI records, call logs, settings |
| **Infrastructure** | Bicep (IaC) | Provisions and configures all Azure resources |

---

## High-Level Architecture Diagram

```
 ┌───────────────────────────────────────────────────────────────────────────────┐
 │                              Azure Cloud                                     │
 │                                                                               │
 │  Inbound Call (Teams Direct Routing / PSTN)                                   │
 │                                                                               │
 │   PSTN ──────► ┌──────────────────┐     commsNotification                   │
 │   (Direct       │ Microsoft Teams  │────────────────────────────────────────► │
 │    Routing)     │ Phone System     │                                          │
 │                 └──────────────────┘                                          │
 │                                                 │                            │
 │                                                 ▼                            │
 │                ┌──────────────────┐     ┌───────────────────────────────┐    │
 │                │ Azure Cognitive  │◄───►│ Azure Functions (IVR Engine)  │    │
 │                │ Services (Speech)│     │                               │    │
 │                └──────────────────┘     │  ┌─ TeamsCallBot              │    │
 │                                        │  │  POST /api/bot-messages    │    │
 │                ┌──────────────────┐    │  ├─ TranscriptRoutingService   │    │
 │                │ Azure OpenAI     │◄──►│  ├─ ExternalSystemIntegration  │    │
 │                │ (GPT-4o)         │    │  ├─ CallFlowEngine             │    │
 │                └──────────────────┘    │  ├─ AniAliService              │    │
 │                                        │  └─ PromptService              │    │
 │                ┌──────────────────┐    └──────────────┬────────────────┘    │
 │                │ Blob Storage     │◄──►               │                      │
 │                │ (Audio Prompts)  │                   │                      │
 │                └──────────────────┘                   │                      │
 │                                                       ▼                      │
 │  ┌──────────────────┐                   ┌──────────────────────────────┐    │
 │  │ App Service       │─────────────────►│ Azure Cosmos DB (Serverless) │    │
 │  │ (Blazor Admin     │                  │                              │    │
 │  │  Portal)          │                  │  ├─ AniRecords               │    │
 │  └──────────────────┘                   │  ├─ AliRecords               │    │
 │                                          │  ├─ Menus                    │    │
 │  ┌──────────────────┐                   │  ├─ Prompts                  │    │
 │  │ Azure AD          │                  │  ├─ CallLogs (90-day TTL)    │    │
 │  │ (Admin Auth)      │                  │  ├─ Config                   │    │
 │  └──────────────────┘                   │  ├─ TeamRouting              │    │
 │                                          │  ├─ ExternalSystems         │    │
 │                                          │  ├─ DataExtraction           │    │
 │  ┌──────────────────┐                   │  └─ PhoneNumbers             │    │
 │  │ Application       │                  └──────────────────────────────┘    │
 │  │ Insights          │                                                      │
 │  └──────────────────┘                                                       │
 │                                                                               │
 │                           ┌──────────────────────────────┐                   │
 │               HTTP ──────►│ External Systems (REST APIs) │                   │
 │                           │ Fire Alarm Panels, CAD,      │                   │
 │                           │ Work Order Platforms, etc.    │                   │
 │                           └──────────────────────────────┘                   │
 └───────────────────────────────────────────────────────────────────────────────┘
```

---

## Call Flow Lifecycle

Every inbound call follows this sequence:

### 1. Call Arrival
- A caller dials a phone number provisioned in **Microsoft Teams Phone System** via Direct Routing.
- Teams Phone System emits a `commsNotification` (incoming call) to the IVR bot endpoint via the **Microsoft Graph Calling API**.

### 2. TeamsCallBot (HTTP Trigger — `/api/bot-messages`)
- Receives the Microsoft Graph `commsNotification`.
- Extracts `callerNumber` and `calledNumber` from the notification resource.
- Performs **ANI/ALI lookup** via `AniAliService` to identify the caller (name, account, VIP status, location).
- Checks if the caller is **blocked** — if yes, rejects the call immediately.
- Creates a `CallLog` record in Cosmos DB.
- **Answers the call** via `PATCH /communications/calls/{callId}` on the Microsoft Graph API.
- Determines the root menu and plays the welcome prompt via TTS (Cognitive Services → Blob Storage SAS URL).

### 3. Subsequent Call Events (same `/api/bot-messages` endpoint)
All mid-call events arrive as further `commsNotification` POSTs to the same endpoint:

| Notification | Handler |
|---|---|
| Call established | Resolves the appropriate menu via `CallFlowEngine`, plays the welcome prompt, starts tone subscription |
| Tone received (DTMF) | Matches key to `MenuOption.DtmfKey`, executes action via `ExecuteActionAsync` |
| Play prompt completed | Subscribes to tones / starts record operation for next input |
| Record completed | Extracts transcript from `clientContext`, routes via `TranscriptRoutingService` |
| Call terminated | Finalizes the `CallLog` (end time, duration, disposition) |

### 4. Menu Resolution (`CallFlowEngine`)
The engine determines which menu to present based on a priority waterfall:

1. **Per-DID routing** — if the called number has a `PhoneNumberConfig` with a `RootMenuId`, use that menu
2. **Blocked caller** — reject immediately
3. **VIP routing** — if ANI record has `IsVip = true` and a `CustomRoutingMenuId`
4. **Holiday routing** — if today matches a holiday in `BusinessHoursConfig`
5. **After-hours routing** — if outside configured business hours
6. **Conditional routing** — menu-level conditions (caller type, region, VIP status)
7. **Default** — the system-wide root menu

### 5. Input Collection
Each menu supports one of two input modes:

- **DTMF mode** (`EnableSpeechRecognition = false`): Collects touch-tone digits. Matched against `MenuOption.DtmfKey`.
- **Speech mode** (`EnableSpeechRecognition = true`): Captures free-form speech via Azure Cognitive Services. Transcript is sent to Azure OpenAI for intent classification.

### 6. Action Execution
After input is matched, the system executes the associated `MenuAction`:

| Action Type | Behavior |
|---|---|
| `NavigateToMenu` | Loads a sub-menu and plays its prompt |
| `TransferToNumber` | Transfers the call to an external phone number |
| `TransferToTeam` | AI-classified transfer to a team's phone number |
| `PlayPrompt` | Plays a TTS/audio/SSML prompt |
| `Hangup` | Optionally plays goodbye prompt, then disconnects |
| `RepeatMenu` | Replays the current menu |
| `Webhook` | Fires an HTTP POST with call context to a URL |
| `SubmitToExternalSystem` | AI-extracts data from transcript, POSTs to an external REST API |

### 7. Call Logging
Every call is logged in Cosmos DB with:
- Caller/called numbers, ANI/ALI data
- Full menu navigation path (every menu visited and input given)
- Transcript (for speech-routed calls)
- Detected intent, confidence score, and routed team
- Extracted data and external system submission results
- Start/end times, duration, status, disposition

---

## Project Structure

```
ivr-system/
├── src/
│   ├── IVR.Core/                         # Shared class library
│   │   ├── Models/                       # Domain models
│   │   │   ├── AniRecord.cs              # Caller identification (ANI)
│   │   │   ├── AliRecord.cs              # Caller location (ALI)
│   │   │   ├── IvrMenu.cs               # Menu tree, options, actions, conditions
│   │   │   ├── IvrPrompt.cs             # TTS/audio/SSML prompt definitions
│   │   │   ├── CallLog.cs              # Call history with full context
│   │   │   ├── BusinessHoursConfig.cs   # Business hours, holidays, timezone
│   │   │   ├── SystemConfig.cs          # Global IVR settings
│   │   │   ├── TeamRoutingConfig.cs     # AI team routing definitions
│   │   │   ├── ExternalSystemConfig.cs  # External REST API definitions
│   │   │   ├── DataExtractionConfig.cs  # AI field extraction schemas
│   │   │   └── PhoneNumberConfig.cs     # Per-DID PSTN/SBC configuration
│   │   ├── Interfaces/                   # Service contracts
│   │   │   ├── ICosmosDbService.cs      # Data access interface
│   │   │   ├── IBlobStorageService.cs   # Audio file storage interface
│   │   │   └── ICallFlowEngine.cs       # Menu resolution/routing interface
│   │   └── Services/                     # Service implementations
│   │       ├── CosmosDbService.cs       # Cosmos DB CRUD (10 containers)
│   │       └── BlobStorageService.cs    # Blob Storage audio operations
│   │
│   ├── IVR.Functions/                    # Azure Functions app (IVR engine)
│   │   ├── Program.cs                   # Host builder, DI configuration
│   │   ├── host.json                    # Functions runtime config
│   │   ├── local.settings.json          # Local dev connection strings
│   │   ├── Functions/
   │   │   └── TeamsCallBot.cs          # HTTP trigger (all Teams calling events)
│   │   └── Services/
│   │       ├── CallFlowEngine.cs        # Menu resolution, conditions, input processing
│   │       ├── AniAliService.cs         # ANI/ALI lookup + phone normalization
│   │       ├── PromptService.cs         # Prompt resolution + SSML builder
│   │       ├── TranscriptRoutingService.cs    # Azure OpenAI intent classification
│   │       └── ExternalSystemIntegrationService.cs  # AI extraction + HTTP dispatch
│   │
│   ├── IVR.AdminPortal/                  # Blazor Server admin portal
│   │   ├── Program.cs                   # Host builder, Azure AD auth, DI
│   │   ├── Components/MainLayout.razor  # Sidebar navigation layout
│   │   ├── Pages/
│   │   │   ├── Dashboard/Index.razor    # Real-time analytics dashboard
│   │   │   ├── CallFlows/Index.razor    # Menu tree builder
│   │   │   ├── Prompts/Index.razor      # Prompt CRUD
│   │   │   ├── AniAli/Index.razor       # ANI/ALI record management
│   │   │   ├── CallLogs/Index.razor     # Call history viewer
│   │   │   └── Settings/Index.razor     # Business hours, system config
│   │   └── Services/
│   │       └── AdminDashboardService.cs # Dashboard aggregation queries
│   │
│   └── IVR.sln                          # Solution file
│
├── infra/                                # Bicep infrastructure-as-code
│   ├── main.bicep                       # Orchestrator (deploys all modules)
│   ├── modules/
│   │   ├── cosmos-db.bicep              # Cosmos DB account + 10 containers
│   │   ├── storage.bicep                # Storage account + blob containers
│   │   ├── communication-services.bicep # Bot/Teams service settings
│   │   ├── cognitive-services.bicep     # Speech Services
│   │   ├── openai.bicep                 # Azure OpenAI + GPT-4o deployment
│   │   ├── function-app.bicep           # Function App + plan + settings
│   │   ├── app-service.bicep            # App Service for admin portal
│   │   └── app-insights.bicep           # Application Insights + Log Analytics
│   └── parameters/
│       └── dev.bicepparam               # Dev environment parameters
│
├── tests/
│   ├── IVR.Core.Tests/                  # Core library unit tests
│   └── IVR.Functions.Tests/             # Functions integration tests
│
└── docs/                                # Documentation (you are here)
```

---

## Azure Services Used

| Service | Purpose | SKU/Tier |
|---|---|---|
| **Microsoft Teams Phone System** | PSTN call reception via Direct Routing; Microsoft Graph Calling API for call control | Included with Teams license |
| **Azure Functions** | Serverless compute for IVR engine; HTTP trigger at `/api/bot-messages` | Consumption plan (.NET 8 isolated) |
| **Azure Cosmos DB** | NoSQL database for all configuration and call data | Serverless |
| **Azure Blob Storage** | Audio file storage for recorded prompts | Standard LRS |
| **Azure Cognitive Services** | Speech-to-text for real-time call transcription | S0 |
| **Azure OpenAI** | GPT-4o for intent classification and data extraction | Standard (gpt-4o deployment) |
| **Azure App Service** | Hosts the Blazor Server admin portal | B1 or higher |
| **Azure AD (Entra ID)** | Authentication for admin portal (role-based) | Included |
| **Application Insights** | Telemetry, logging, and monitoring | Pay-as-you-go |

---

## Cosmos DB Data Model

The system uses a single Cosmos DB database (`IvrDatabase`) with **10 containers**:

| Container | Partition Key | TTL | Purpose |
|---|---|---|---|
| `AniRecords` | `/partitionKey` (first 4 digits of phone) | — | Caller identification records |
| `AliRecords` | `/partitionKey` (first 4 digits of phone) | — | Caller location records |
| `Menus` | `/partitionKey` ("menu") | — | IVR menu tree nodes |
| `Prompts` | `/partitionKey` ("prompt") | — | TTS/audio/SSML prompt definitions |
| `CallLogs` | `/partitionKey` (yyyy-MM) | 90 days | Call history with full context |
| `Config` | `/partitionKey` ("config") | — | System config, business hours |
| `TeamRouting` | `/partitionKey` ("team-routing") | — | AI team routing definitions |
| `ExternalSystems` | `/partitionKey` ("external-system") | — | External REST API connection configs |
| `DataExtraction` | `/partitionKey` ("data-extraction") | — | AI field extraction schemas |
| `PhoneNumbers` | `/partitionKey` ("phone-number") | — | Per-DID PSTN / SBC / direct routing config |

### Partition Key Strategy
- **ANI/ALI**: Partitioned by the first 4 digits of the phone number for even distribution across high-volume lookups.
- **Menus/Prompts/Config**: Use a fixed partition key since these are low-volume, admin-managed datasets.
- **CallLogs**: Partitioned by `yyyy-MM` (year-month) for efficient time-range queries and natural data aging.
- **TeamRouting/ExternalSystems/DataExtraction/PhoneNumbers**: Fixed partition keys since these are configuration data with low write volume.

---

## Dependency Injection

All services are registered in `Program.cs` as singletons for performance in the serverless environment:

```csharp
// Azure SDK clients
services.AddSingleton(new CallAutomationClient(acsConnectionString));
services.AddSingleton(new CosmosClient(cosmosConnectionString, ...));
services.AddSingleton(new BlobServiceClient(storageConnectionString));
services.AddSingleton(new AzureOpenAIClient(endpoint, credential));

// Application services
services.AddSingleton<ICosmosDbService, CosmosDbService>();
services.AddSingleton<IBlobStorageService, BlobStorageService>();
services.AddSingleton<ICallFlowEngine, CallFlowEngine>();
services.AddSingleton<AniAliService>();
services.AddSingleton<PromptService>();
services.AddSingleton<TranscriptRoutingService>();
services.AddSingleton<ExternalSystemIntegrationService>();

// HTTP client factory (for external system calls)
services.AddHttpClient("ExternalSystems", client => {
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

---

## Infrastructure Deployment

The entire infrastructure is defined in Bicep and deployed with a single command:

```bash
az deployment group create \
  --resource-group rg-ivr-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam
```

The `main.bicep` orchestrator deploys modules in dependency order and wires outputs (connection strings, endpoints, keys) between resources automatically.

---

## Security

- **Admin Portal**: Protected by Azure AD authentication with role-based authorization (`IVR.Admin` role required).
- **Function App API**: Callback endpoints use anonymous auth (required for ACS callbacks) but are scoped to specific call IDs. Event Grid validates subscriptions via handshake.
- **External System Credentials**: Stored in `ExternalAuthConfig` documents. Production deployments should reference Azure Key Vault secrets rather than storing credentials directly.
- **Cosmos DB**: Connection string authentication (managed identity recommended for production).
- **Azure OpenAI**: API key or managed identity authentication.

---

## Monitoring & Observability

- All components emit structured logs to **Application Insights**.
- Every call event is logged with correlation IDs for end-to-end tracing.
- The admin portal **Dashboard** provides real-time metrics:
  - Today's call count, average duration, active menus, open/closed status
  - Call disposition breakdown (weekly)
  - Calls-by-hour histogram (today)
  - Recent calls table with quick detail access

---

## Related Documentation

- [PSTN & CM10 Simulator](pstn-simulator.md) — Standalone testing tool with Mock/Live ACS modes
- [CM10 Setup Guide](cm10-setup-guide.md) — Avaya CM10 VDN/vector configuration and Direct Routing setup
- [System Integration](system-integration.md) — External systems, team routing, and CM10 VDN transfer
- [Prompt Management](prompt-management.md) — TTS, SSML, and audio prompt configuration
- [Admin Portal](admin-portal.md) — Web UI guide for managing the IVR
