# E911 IVR — Local Demo Guide (Teams Bot Mode)

This guide covers how to run a full end-to-end demo of the Teams-integrated IVR
**without needing access to a real Microsoft Teams tenant**. The PSTN Simulator
replaces Teams Phone System and the Microsoft Graph Calling API, letting you drive
calls, press DTMF keys, and speak phrases entirely from a browser.

---

## Architecture in Demo Mode

```
Your Browser
  |
  +--> PSTN Simulator (localhost:5200)   <-- you click "Make Call" here
         |
         | commsNotifications JSON
         v
  IVR Functions (localhost:7071)  /api/bot-messages
         |
         | Graph API calls  -->  PSTN Simulator (localhost:5200) /graph/v1.0/*
         |                       (simulator responds + fires follow-up events)
         v
  Admin Portal (localhost:8080)   <-- you watch call logs, menus, ANI/ALI here
```

---

## Prerequisites

- Docker Desktop running
- Repository cloned and on the `teams_integration` branch
- (Optional) Real Cosmos DB + Azure OpenAI connection strings for full demo
  - Without them the system uses **in-memory seeded data** — menus, ANI/ALI,
    and team routing all work; call logs are ephemeral

---

## Quick Start (Full Teams Demo)

### Step 1 — Configure `.env`

Copy the example and fill in the optional Azure service strings:

```powershell
cd c:\DSOP\repos3\IVR\e911-ivr
Copy-Item .env.example .env   # if not already done
```

Minimum `.env` for a fully-offline demo (all in-memory, no Azure needed):
```env
# Teams Bot (simulator credentials — no real auth in local mode)
TEAMS_BOT_APP_ID=simulator
TEAMS_BOT_APP_PASSWORD=simulator
TEAMS_BOT_TENANT_ID=simulator

# Graph API routes to the simulator (not real Azure Graph)
GRAPH_API_ENDPOINT=http://pstn-simulator:8080/graph/v1.0

# Simulator mode
IVR_MODE=TeamsBot
```

For a richer demo with real Cosmos DB, AI routing, and TTS:
```env
COSMOS_DB_CONNECTION_STRING=AccountEndpoint=https://...
COGNITIVE_SERVICES_ENDPOINT=https://ivr-teams-speech-xxx.cognitiveservices.azure.us/
COGNITIVE_SERVICES_KEY=...
AZURE_OPENAI_ENDPOINT=https://ivr-teams-openai-xxx.openai.azure.us/
AZURE_OPENAI_API_KEY=...
AZURE_OPENAI_DEPLOYMENT=gpt-45
STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;...
```

### Step 2 — Build and Start All Containers

```powershell
docker compose up --build -d
```

Verify all four containers are running:
```powershell
docker compose ps
```

| Container | URL | What it does |
|---|---|---|
| `ivr-functions` | http://localhost:7071 | IVR engine / Teams calling bot |
| `ivr-admin` | http://localhost:8080 | Admin portal (watch this during demo) |
| `pstn-simulator` | http://localhost:5200 | Drives the simulated call |
| `azurite` | localhost:10000-10002 | Local storage emulator |

### Step 3 — Open Two Tabs

- **Tab 1**: http://localhost:5200 — PSTN Simulator (your "phone")
- **Tab 2**: http://localhost:8080 — Admin Portal (observe live call state)

---

## Demo Walkthrough — Step by Step

### Scene 1: Basic DTMF Call Flow

**Goal:** Show a caller dialing in, navigating the E911 menu with keypad presses,
and the call log being recorded.

1. In the **Simulator** (Tab 1), click **New Call**
2. Select a caller from the pre-loaded ANI records (e.g., "John Smith - VIP")
3. Select DID `+17035550911` (E911 Main Line)
4. Click **Place Call**
   - Simulator sends `commsNotification (incoming)` to the IVR
   - IVR answers via mock Graph API
   - Welcome prompt plays (IVR sends `playPrompt` → simulator fires `playPromptOperation`)
5. In the **Simulator**, click **DTMF: 1** (Emergency — Transfer to RDC Dispatcher)
   - IVR receives the tone, navigates to the emergency transfer action
   - Call transitions to "Transferred"
6. In the **Admin Portal** (Tab 2), click **Call Logs**
   - Find the call — it shows caller number, ANI data (John Smith), menu path taken, disposition

### Scene 2: Speech / AI Routing

**Goal:** Show AI-powered natural language routing.

1. Place a new call from the Simulator
2. Select a speech-routing DID or wait for the main menu to reach a speech prompt
3. Click **Speak** in the Simulator and type: `"There is a fire in Building 7"`
4. The IVR:
   - Sends the transcript to Azure OpenAI (or returns a seeded match in offline mode)
   - Classifies intent: "Fire Dispatcher" at high confidence
   - Plays: "I'll connect you with our Fire Dispatch team now"
   - Transfers the call
5. Call Log shows:
   - `transcript: "There is a fire in Building 7"`
   - `detectedIntent: "Fire Dispatcher"`
   - `intentConfidence: 0.94`

### Scene 3: ANI/ALI Demo

**Goal:** Show caller identification and location data.

1. In the **Admin Portal**, go to **ANI/ALI** and show the pre-seeded caller records
2. Place a call from the Simulator using a VIP caller
3. In the Call Logs, show how the IVR auto-identified the caller name, location, and VIP status

### Scene 4: Menu Management

**Goal:** Show the admin portal's menu builder.

1. In the **Admin Portal**, click **Call Flows**
2. Show the E911 Main Menu tree with all 4 options
3. Demonstrate editing a menu option label or DTMF key
4. Save — the change takes effect on the next call immediately (no redeploy)

---

## Switching to Real Teams (When Customer Tenant is Available)

When you have access to the customer's Teams tenant, update `.env`:

```env
# Real Teams bot credentials (from Create-BotAppRegistration.ps1 output)
TEAMS_BOT_APP_ID=251948e8-7012-4fc4-a6b6-59e82c9dd983
TEAMS_BOT_APP_PASSWORD=<real-secret>
TEAMS_BOT_TENANT_ID=5af05be5-b9df-43d4-8897-ec17d3118935

# Real Azure Government Graph API (not the simulator)
GRAPH_API_ENDPOINT=https://graph.microsoft.us/v1.0
TEAMS_CHANNEL_SERVICE=https://botframework.azure.us

# Switch simulator to not intercept Graph calls
IVR_MODE=Live   # remove once real Teams is configured
```

Then run:
```powershell
docker compose up --build -d
```

---

## Troubleshooting

| Symptom | Check |
|---|---|
| Simulator shows "IVR rejected event" | IVR Functions not running — check `docker compose ps` |
| Call stuck at "incoming", never "established" | GraphApiEndpoint not pointing to simulator — check `.env` |
| No menu plays (silent after answer) | TTS service not configured — prompts need audio URL or Cognitive Services key |
| AI routing returns "no match" | OpenAI not configured — falls back to DTMF routing |
| Admin Portal blank / no data | Cosmos DB not connected — running in in-memory mode, data is seeded but ephemeral |

```powershell
# View live logs for any container
docker compose logs -f ivr-functions
docker compose logs -f pstn-simulator
```
