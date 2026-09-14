# IVR System — Teams-Integrated

A multi-level IVR (Interactive Voice Response) system for e611-IVR call handling, built on Azure. Inbound PSTN calls are received by **Microsoft Teams Phone System** and routed to an **Azure Functions calling bot** that drives ANI/ALI lookup, configurable menu navigation, AI-powered intent routing, and external system integration — all managed through a Blazor Server admin portal.

> **Architecture change (July 2026):** Azure Communication Services (ACS) has been replaced with a Microsoft Teams Calling Bot + Microsoft Graph API. The IVR business logic, Cosmos DB schema, OpenAI integration, and Admin Portal are unchanged.

## Architecture

```
PSTN Carrier
     |
Teams Phone System  (Calling Plans or Operator Connect — no customer SBC needed)
     |
Teams Resource Account  (holds the phone number)
     |
Azure Bot Service  (routes incoming call notification to Function App)
     |
Azure Functions — TeamsCallBot  (HTTP trigger /api/bot-messages)
├─ ANI/ALI lookup  ─► Cosmos DB
├─ Answers call    ─► Microsoft Graph API  POST /communications/calls/{id}/answer
├─ Plays prompts   ─► Microsoft Graph API  POST .../playPrompt
│   ├─ TTS text    ─► Azure Cognitive Services (Neural Voice)
│   └─ Audio file  ─► Azure Blob Storage (.wav/.mp3)
├─ Collects DTMF   ─► Microsoft Graph API  POST .../recordResponse
├─ Speech input    ─► Azure Cognitive Services (STT) ─► transcript string
├─ AI routing      ─► Azure OpenAI GPT-4.1  (intent classification)
├─ Call transfer   ─► Microsoft Graph API  POST .../transfer
└─ Call logs       ─► Cosmos DB
```

```
+-------------------------------------------------------------------+
|                          Azure Cloud                              |
|                                                                   |
|  +------------------+    +-------------------------------------+  |
|  | Azure Bot Service|───►| Azure Functions (IVR Engine)        |  |
|  | (channel reg.)   |    |                                     |  |
|  +------------------+    | • TeamsCallBot  (Bot Framework)     |  |
|                           | • CallFlowEngine                    |  |
|  +------------------+    | • AniAliService                     |  |
|  | Microsoft Graph  |◄──►| • TranscriptRoutingService          |  |
|  | Calling API      |    | • ExternalSystemIntegration         |  |
|  +------------------+    | • PromptService                     |  |
|                           +------------------+------------------+  |
|  +------------------+                       |                     |
|  | Cognitive Svcs   |◄──────────────────────+  TTS / STT         |
|  | (Speech)         |                       |                     |
|  +------------------+                       v                     |
|                           +-------------------------------------+  |
|  +------------------+    | Cosmos DB (Serverless)              |  |
|  | Azure Blob       |◄──►| • AniRecords   • Menus              |  |
|  | Storage          |    | • AliRecords   • Prompts            |  |
|  | (Audio Prompts)  |    | • CallLogs     • TeamRouting        |  |
|  +------------------+    | • Config       • PhoneNumbers       |  |
|                           +-------------------------------------+  |
|  +------------------+                       ^                     |
|  | Azure OpenAI     |◄──────────────────────+  AI intent         |
|  | (GPT-4.1)        |                                             |
|  +------------------+    +-------------------------------------+  |
|                           | App Service (Blazor Admin Portal)   |  |
|  +------------------+    | Manages menus, prompts, ANI/ALI,    |  |
|  | Entra ID (Auth)  |───►| call logs, team routing, settings   |  |
|  +------------------+    +-------------------------------------+  |
|                                                                   |
|  +------------------+                                            |
|  | App Insights     | Telemetry for all components               |
|  +------------------+                                            |
+-------------------------------------------------------------------+
```

## Project Structure

```
e611-ivr/
├── src/
│   ├── IVR.Core/                    # Shared library — zero Azure calling dependencies
│   │   ├── Models/                  # ANI, ALI, CallLog, Menu, Prompt, TeamRouting models
│   │   ├── Services/                # Cosmos DB, Blob Storage services
│   │   └── Interfaces/              # Service contracts
│   │
│   ├── IVR.Functions/               # Azure Functions — IVR engine + Teams calling bot
│   │   ├── Functions/
│   │   │   └── TeamsCallBot.cs      # Bot Framework HTTP trigger (/api/bot-messages)
│   │   │                            # Handles: incoming call, connected, DTMF, speech,
│   │   │                            #          play completed, transfer, disconnect
│   │   └── Services/
│   │       ├── AniAliService              # ANI/ALI lookup & caller resolution
│   │       ├── CallFlowEngine             # Menu routing, conditions, business hours
│   │       ├── PromptService              # TTS / audio prompt resolution
│   │       ├── TranscriptRoutingService   # Azure OpenAI speech intent classification
│   │       └── ExternalSystemIntegration  # REST dispatch to external APIs
│   │
│   ├── IVR.AdminPortal/             # Blazor Server admin portal
│   │   └── Pages/
│   │       ├── Dashboard/           # Call analytics & system status
│   │       ├── Prompts/             # TTS/audio prompt management
│   │       ├── CallFlows/           # Visual menu tree builder
│   │       ├── AniAli/              # ANI/ALI record CRUD
│   │       ├── CallLogs/            # Call history & detail viewer
│   │       ├── Teams/               # Team routing configuration
│   │       └── Settings/            # Business hours, holidays, system config
│   │
│   └── IVR.sln
│
├── infra/                           # Bicep IaC templates
│   ├── main.bicep                   # Main orchestrator
│   ├── modules/
│   │   ├── bot-service.bicep        # Azure Bot Service + Teams channel (calling enabled)
│   │   ├── cosmos-db.bicep          # Cosmos DB + containers
│   │   ├── storage.bicep            # Blob storage for audio prompts
│   │   ├── cognitive-services.bicep # Speech TTS/STT
│   │   ├── openai.bicep             # Azure OpenAI (GPT-4.1)
│   │   ├── function-app.bicep       # Function App hosting
│   │   ├── app-service.bicep        # Admin portal hosting
│   │   └── app-insights.bicep       # Monitoring
│   └── parameters/
│       ├── azuregov.bicepparam      # Azure Government (GCC High) parameters
│       └── dev.bicepparam           # Commercial dev parameters
│
├── scripts/
│   ├── Create-BotAppRegistration.ps1  # Creates Entra App Registration for the bot
│   └── README.md
│
└── docs/                            # Full documentation
```

## Azure Services Required

| Service | Purpose | Notes |
|---|---|---|
| **Azure Bot Service** (S1) | Registers calling bot, enables Teams channel | Deployed via Bicep |
| **Azure Functions** (v4) | IVR engine — TeamsCallBot + all IVR services | .NET 8 isolated worker |
| **Microsoft Graph API** | Call control (answer, play, DTMF, transfer, hangup) | App-only auth via Entra |
| **Teams Phone System** | PSTN reception via Calling Plans or Operator Connect | M365 admin config |
| **Cosmos DB** (Serverless) | All IVR configuration and call logs | |
| **Azure Blob Storage** | Pre-recorded audio prompt files (.wav/.mp3) | |
| **Azure Cognitive Services** | TTS (Neural Voice) + STT (speech recognition) | |
| **Azure OpenAI** (GPT-4.1) | AI intent classification for speech-routed calls | |
| **App Service** | Blazor admin portal | |
| **Entra ID** | Admin portal auth + Bot App Registration | Run Create-BotAppRegistration.ps1 |
| **Application Insights** | Telemetry + logging | |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- Microsoft 365 tenant with **Teams Phone System** license (E5 or Phone System add-on)
- Azure subscription (commercial or Azure Government / GCC High)

## Getting Started

### 1. Create the Bot App Registration

Run this **before** deploying infrastructure. Creates the Entra App Registration with the required Microsoft Graph calling permissions (`Calls.Initiate.All`, `Calls.AccessMedia.All`, etc.).

```powershell
# Azure Government (GCC High)
az cloud set --name AzureUSGovernment
az login
cd scripts
.\Create-BotAppRegistration.ps1 -Environment AzureUSGovernment
```

Copy the output `teamsBotAppId` and `teamsBotAppPassword` into your `.bicepparam` file.

### 2. Deploy Infrastructure

```powershell
az group create --name rg-ivr-dev --location usgovarizona

az deployment group create `
  --resource-group rg-ivr-dev `
  --template-file infra/main.bicep `
  --parameters infra/parameters/azuregov.bicepparam
```

### 3. Configure Teams Phone System (M365 Admin Center)

1. Create a **Resource Account** and assign it the phone number
2. Assign a **Calling Policy** to the Resource Account that allows bot calls
3. Set the **calling webhook** to the bot messaging endpoint from the Bicep deployment output:
   `https://<functionapp>.azurewebsites.us/api/bot-messages`

### 4. Configure Local Development

Create `src/IVR.Functions/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "MicrosoftAppType": "SingleTenant",
    "MicrosoftAppId": "<bot-app-id>",
    "MicrosoftAppPassword": "<bot-app-secret>",
    "MicrosoftAppTenantId": "<tenant-id>",
    "ChannelService": "https://botframework.azure.us",
    "GraphApiEndpoint": "https://graph.microsoft.us/v1.0",
    "CosmosDbConnectionString": "<cosmos-connection-string>",
    "CognitiveServicesEndpoint": "<speech-endpoint>",
    "AzureOpenAI__Endpoint": "<openai-endpoint>",
    "AzureOpenAI__ApiKey": "<openai-key>",
    "AzureOpenAI__DeploymentName": "gpt-41"
  }
}
```

### 5. Run Locally

```bash
# Terminal 1: Run Functions
cd src/IVR.Functions
func start

# Terminal 2: Run Admin Portal
cd src/IVR.AdminPortal
dotnet run
```

## Call Flow

1. Caller dials the DID assigned to the Teams Resource Account
2. **Teams Phone System** receives the PSTN call (Calling Plan or Operator Connect)
3. **Azure Bot Service** delivers an `onIncomingCall` activity to `/api/bot-messages`
4. **TeamsCallBot** — ANI/ALI lookup, blocked check, answers call via Graph API
5. **Graph API** confirms `callConnected` — bot plays welcome prompt (TTS or audio file)
6. **Graph API** collects DTMF or speech from caller
7. **CallFlowEngine** navigates the menu tree based on caller input
8. Speech input — **Azure OpenAI** classifies intent and routes to matched team
9. **Graph API** transfers call to Teams user, Call Queue, or PSTN number
10. **Cosmos DB** call log updated with full path, transcript, intent, and disposition

## Documentation

Full documentation is in the [docs/](docs/) folder:

- **[Architecture Overview](docs/architecture.md)** — System design and call flow lifecycle
- **[System Integration](docs/system-integration.md)** — AI routing, external systems, team routing
- **[Admin Portal](docs/admin-portal.md)** — Managing menus, prompts, ANI/ALI, call logs
- **[Azure Government Deployment](docs/azure-gov-deployment.md)** — GCC High specific notes
- **[Start Local Development](docs/startlocal.md)** — Running locally
- **[PSTN Simulator](docs/pstn-simulator.md)** — Test without real phone calls

## License

MIT
