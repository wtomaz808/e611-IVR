# IVR System — Azure Integrated

A multi-level IVR (Interactive Voice Response) system built on Azure with ANI/ALI data support and a Blazor Server admin portal.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Azure Cloud                              │
│                                                                 │
│  ┌──────────────┐    ┌───────────────┐    ┌──────────────────┐  │
│  │ Azure Comm.  │───▶│ Azure Event   │───▶│ Azure Functions  │  │
│  │ Services     │    │ Grid          │    │ (IVR Engine)     │  │
│  │ (PSTN/SIP)   │    └───────────────┘    │                  │  │
│  └──────────────┘                         │ • Call Handler   │  │
│                                           │ • ANI/ALI Lookup │  │
│  ┌──────────────┐                         │ • Menu Engine    │  │
│  │ Cognitive    │◀────────────────────────│ • DTMF/Speech    │  │
│  │ Services     │                         └────────┬─────────┘  │
│  │ (TTS/STT)   │                                   │            │
│  └──────────────┘                                   ▼            │
│                                           ┌──────────────────┐  │
│  ┌──────────────┐                         │ Cosmos DB        │  │
│  │ Blob Storage │◀────────────────────────│ • ANI Records    │  │
│  │ (Audio       │                         │ • ALI Records    │  │
│  │  Prompts)    │                         │ • Menus/Prompts  │  │
│  └──────────────┘                         │ • Call Logs      │  │
│                                           │ • Config         │  │
│  ┌──────────────┐                         └──────────────────┘  │
│  │ App Service  │                                   ▲            │
│  │ (Blazor      │───────────────────────────────────┘            │
│  │  Admin)      │                                                │
│  └──────────────┘                                                │
│                                                                 │
│  ┌──────────────┐    ┌───────────────┐                          │
│  │ Azure AD     │    │ App Insights  │                          │
│  │ (Auth)       │    │ (Monitoring)  │                          │
│  └──────────────┘    └───────────────┘                          │
└─────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
ivr-system/
├── src/
│   ├── IVR.Core/                    # Shared library
│   │   ├── Models/                  # ANI, ALI, CallLog, Menu, Prompt models
│   │   ├── Services/                # Cosmos DB, Blob Storage services
│   │   └── Interfaces/              # Service contracts
│   │
│   ├── IVR.Functions/               # Azure Functions (IVR engine)
│   │   ├── Functions/
│   │   │   ├── IncomingCallHandler  # Event Grid trigger for incoming calls
│   │   │   └── CallbackHandler      # HTTP callback for call events
│   │   └── Services/
│   │       ├── AniAliService        # ANI/ALI lookup & caller resolution
│   │       ├── CallFlowEngine       # Menu routing, conditions, business hours
│   │       └── PromptService        # TTS/audio prompt resolution
│   │
│   ├── IVR.AdminPortal/             # Blazor Server admin portal
│   │   └── Pages/
│   │       ├── Dashboard/           # Call analytics & system status
│   │       ├── Prompts/             # TTS/audio prompt management
│   │       ├── CallFlows/           # Visual menu tree builder
│   │       ├── AniAli/              # ANI/ALI record CRUD
│   │       ├── CallLogs/            # Call history & detail viewer
│   │       └── Settings/            # Business hours, holidays, system config
│   │
│   └── IVR.sln
│
├── infra/                           # Bicep IaC templates
│   ├── main.bicep                   # Main orchestrator
│   ├── modules/
│   │   ├── cosmos-db.bicep          # Cosmos DB + containers
│   │   ├── storage.bicep            # Blob storage for audio
│   │   ├── communication-services   # Azure Communication Services
│   │   ├── cognitive-services       # Speech TTS/STT
│   │   ├── function-app.bicep       # Function App hosting
│   │   ├── app-service.bicep        # Admin portal hosting
│   │   └── app-insights.bicep       # Monitoring
│   └── parameters/
│       └── dev.bicepparam
│
└── .github/workflows/deploy.yml     # CI/CD pipeline
```

## Features

### IVR Engine
- **ANI/ALI Lookup** — Automatic caller identification and location resolution
- **Multi-Level Menus** — Nested menu trees with DTMF and speech input
- **Conditional Routing** — Route by caller type, VIP status, region, business hours
- **Business Hours** — Time-based routing with after-hours and holiday menus
- **VIP Routing** — Custom call flows for priority callers
- **Blocked Callers** — Automatic call rejection for blocked numbers

### Admin Portal
- **Dashboard** — Real-time call stats, disposition charts, hourly volume
- **Prompt Manager** — Create/edit TTS text, upload audio files, SSML support
- **Call Flow Builder** — Visual menu tree with drag-and-drop options
- **ANI/ALI Manager** — CRUD for caller records, bulk CSV import
- **Call Logs** — Searchable history with full menu path tracking
- **Settings** — Business hours, holidays, global config

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- Azure subscription with these services enabled:
  - Azure Communication Services (with PSTN numbers)
  - Azure Cognitive Services (Speech)
  - Azure Cosmos DB
  - Azure Blob Storage

## Getting Started

### 1. Deploy Infrastructure

```bash
az login
az group create --name rg-ivr-dev --location eastus

az deployment group create \
  --resource-group rg-ivr-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam
```

### 2. Configure Local Development

Update connection strings in:
- `src/IVR.Functions/local.settings.json`
- `src/IVR.AdminPortal/appsettings.json`

### 3. Run Locally

```bash
# Terminal 1: Run Functions
cd src/IVR.Functions
func start

# Terminal 2: Run Admin Portal
cd src/IVR.AdminPortal
dotnet run
```

### 4. Configure Event Grid

Register the Function App endpoint as an Event Grid subscription for your Azure Communication Services resource's `Microsoft.Communication.IncomingCall` event.

### 5. Configure PSTN

Purchase a phone number in Azure Communication Services and route it to trigger the IVR.

## Call Flow

1. **Incoming call** arrives at Azure Communication Services
2. **Event Grid** triggers the `IncomingCallHandler` function
3. **ANI/ALI lookup** identifies the caller and their location
4. **Business hours check** determines if the office is open
5. **Menu resolution** selects the appropriate IVR menu based on conditions
6. **Call answered** and welcome prompt played
7. **DTMF/speech collection** captures caller input
8. **Menu action executed** — navigate, transfer, play, hangup
9. **Call log recorded** with full menu path and ANI/ALI data

## Documentation

Comprehensive documentation is available in the [docs/](docs/) folder:

### Core Documentation
- **[Architecture Overview](docs/architecture.md)** — System design, call flow lifecycle, component responsibilities
- **[Function App Guide](docs/function-app.md)** — Azure Functions deployment and configuration
- **[System Integration](docs/system-integration.md)** — AI routing, external systems, PSTN connectivity, Avaya CM10 integration
- **[Admin Portal](docs/admin-portal.md)** — Web UI for managing menus, prompts, ANI/ALI, call logs

### Configuration & Setup
- **[Azure Communication Services Configuration](docs/acs-configuration-guide.md)** — Complete guide for ACS setup, Event Grid, Direct Routing, SBC configuration
- **[Custom Domain Quick Reference](docs/acs-custom-domain-quickref.md)** — Quick start for custom domain verification
- **[Azure Government Deployment](docs/azure-gov-deployment.md)** — Azure Government-specific deployment notes
- **[CM10 Setup Guide](docs/cm10-setup-guide.md)** — Avaya Communication Manager 10 integration

### Azure Government Important Notes
- **[Direct Routing Limitations](docs/azure-gov-direct-routing-limitation.md)** — Known limitations of ACS Direct Routing in Azure Government Cloud

### Scripts
- **[Configure-AcsCustomDomain.ps1](scripts/Configure-AcsCustomDomain.ps1)** — Automate custom domain setup for ACS Direct Routing

### Development & Testing
- **[Start Local Development](docs/startlocal.md)** — Running the IVR system locally
- **[PSTN Simulator](docs/pstn-simulator.md)** — Test IVR without real phone calls

## License

MIT
