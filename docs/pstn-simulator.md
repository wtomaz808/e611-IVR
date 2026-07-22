# PSTN Simulator

A standalone Blazor Server application that simulates the Microsoft Teams Phone System and the Microsoft Graph Calling API for end-to-end testing of the IVR system — **no Azure or telephony hardware required**.

## Architecture

```
┌──────────────────────────────────────────────────┐
│               PSTN Simulator                     │
│                                                  │
│  ┌──────────┐   ┌──────────┐   ┌──────────────┐ │
│  │  Phone    │   │   CM10   │   │  Mock Teams  │ │
│  │  Dialer   │   │  Engine  │   │  Graph API   │ │
│  │  (UI)     │   │          │   │  /graph/v1.0 │ │
│  └────┬─────┘   └────┬─────┘   └──────┬───────┘ │
│       │              │                 │         │
│  ┌────┴──────────────┴─────────────────┴───────┐ │
│  │           Call State Manager                 │ │
│  │      (in-memory, real-time events)          │ │
│  └─────────────────────┬───────────────────────┘ │
└────────────────────────┼─────────────────────────┘
                         │  commsNotifications + Graph calls
                         ▼
┌──────────────────────────────────────────────────┐
│               IVR Functions                      │
│  (Azure Functions / Docker)                      │
└──────────────────────────────────────────────────┘
```

## Operating Mode (Teams Bot)

The simulator runs a **mock Microsoft Graph Calling API** on the same port as the Blazor UI. The IVR Functions connect to this mock instead of the real Microsoft Graph, enabling fully offline end-to-end testing.

**How it works:**

1. Simulator sends a `commsNotification` (incoming call) to the IVR endpoint (`/api/bot-messages`)
2. IVR calls `PATCH /communications/calls/{callId}` to answer → hits the mock Graph API at `/graph/v1.0/`
3. Mock returns a valid response and sends a `CallEstablished` notification
4. IVR plays prompts → mock captures the prompt URL, fires `playPromptOperation` completion
5. IVR subscribes to tones → mock waits for user DTMF input from the simulator UI
6. IVR transfers → mock routes call to transferred state

**IVR Configuration for Simulator Mode:**

```env
IVR_MODE=TeamsBot
GRAPH_API_ENDPOINT=http://pstn-simulator:8080/graph/v1.0
TEAMS_BOT_APP_ID=simulator
TEAMS_BOT_APP_PASSWORD=simulator
```

## Quick Start

### Option 1: Docker (Recommended)

```bash
cd simulator

# Copy and configure environment
cp .env.example .env

# Build and run
docker compose up --build -d

# Open the simulator UI
open http://localhost:5200
```

### Option 2: Local Development

```bash
cd simulator/src/PstnSimulator

# Run the simulator
dotnet run

# Open http://localhost:5096 (or the port shown in terminal)
```

### Connecting to the IVR

**Same Docker network (recommended):**

```bash
# Create a shared network
docker network create ivr-network

# Start the IVR (from the main project root)
docker compose up -d

# Start the simulator
cd simulator
docker compose up -d
```

Update the IVR's docker-compose environment:

```yaml
environment:
  - GRAPH_API_ENDPOINT=http://pstn-simulator:8080/graph/v1.0
  - CALLBACK_BASE_URL=http://ivr-functions:80
```

**IVR running locally:**

Set `IVR_ENDPOINT=http://host.docker.internal:7071` in the simulator's `.env`.

## UI Pages

### Phone Simulator (`/`)

The main dialer interface:

- **Caller Selection** — Choose from pre-configured callers (Regular, VIP, Blocked)
- **DID Selection** — Choose which phone number to dial
- **DTMF Keypad** — Send tone inputs when the IVR is in recognize mode
- **Speech Input** — Type free-form speech when the IVR uses speech recognition
- **Call Status** — Real-time call state, prompt display, agent assignment
- **Event Trace** — Color-coded timeline of every event in the call lifecycle

### Call Monitor (`/call-monitor`)

Real-time dashboard for all active calls:

- Active call list with state badges
- Per-call event trace with source filtering (PSTN, CM10, IVR, ACS, Agent)
- Recent completed call records (CDRs)

### CM10 Administration (`/cm10-admin`)

Read-only view of the CM10 configuration:

- PSTN trunks with capacity utilization
- DID-to-VDN assignments
- VDN list with destination types
- Call vectors with step-by-step logic
- ACD queue configuration
- Simulated caller directory

### Agent Console (`/agent-console`)

Simulated agent desktop:

- Agent cards with real-time availability status
- State controls (Available, Break, Offline)
- Active call information per agent
- Call release button
- Queue status with availability metrics

## Configuration

All simulation parameters are in `appsettings.json`:

### PSTN Configuration (`Pstn` section)

| Setting | Description |
|---------|-------------|
| `Trunks[].Name` | Trunk identifier |
| `Trunks[].Type` | `PRI` (23 channels) or `SIP` (configurable) |
| `Trunks[].Capacity` | Maximum concurrent channels |
| `Dids[].Number` | DID phone number (E.164) |
| `Dids[].TrunkName` | Which trunk carries this DID |
| `Dids[].VdnNumber` | CM10 VDN to route to |
| `Callers[].Number` | Caller phone number |
| `Callers[].Type` | `Regular`, `VIP`, or `Blocked` |

### CM10 Configuration (`Cm10` section)

| Setting | Description |
|---------|-------------|
| `Vdns[].Number` | VDN extension number |
| `Vdns[].VectorId` | Call vector to execute |
| `Vdns[].Destination` | `IVR`, `Queue:name`, `Announcement:id` |
| `Vectors[].Steps[]` | Vector steps: `Route`, `Queue`, `Wait`, `Goto`, `TimeCheck`, `Announcement`, `Disconnect` |
| `Queues[].Strategy` | `RoundRobin`, `MostIdle`, or `Linear` |
| `Queues[].AgentIds` | Agents assigned to this queue |
| `Agents[].State` | Initial state: `Available`, `OnBreak`, `Offline` |
| `Agents[].Skills` | Agent skill tags |

## Mock Teams Graph API Endpoints

The mock implements these ACS Call Automation REST API endpoints:

| Endpoint | ACS SDK Method | Behavior |
|----------|---------------|----------|
| `POST /calling/callConnections:answer` | `AnswerCallAsync` | Returns connectionId, sends `CallConnected` callback |
| `POST /calling/callConnections/{id}:play` | `PlayToAllAsync` | Captures prompt text, sends `PlayCompleted` after 1.5s |
| `POST /calling/callConnections/{id}:recognize` | `StartRecognizingAsync` | Waits for user DTMF/speech input from UI |
| `POST /calling/callConnections/{id}:transferToParticipant` | `TransferCallAsync` | Routes to CM10 VDN or external |
| `POST /calling/callConnections/{id}:hangUp` | `HangUpAsync` | Finalizes call, releases resources |

## Call Flow Walkthrough

A complete mock call flow:

```
1. User selects caller "+15551234567" and DID "+18005551000"
2. User clicks "Call"
3. PstnService seizes channel on T1-Primary trunk (ch 1/23)
4. Cm10Service looks up VDN 70100 → vector "vec-main" → Route to IVR
5. MockAcsBridge POSTs EventGrid IncomingCall to http://ivr-functions:80/api/incoming-call
6. IVR receives event, calls AnswerCallAsync
7. Mock /calling/callConnections:answer returns connectionId, stores callback URL
8. Mock sends CallConnected callback to IVR
9. IVR plays welcome prompt → Mock captures "Welcome to our IVR..."
10. Mock sends PlayCompleted after 1.5s
11. IVR starts DTMF recognize → UI shows "Press a DTMF key"
12. User clicks "1" on the keypad
13. Mock sends RecognizeCompleted with dtmfResult: ["one"]
14. IVR processes selection, plays next prompt...
15. IVR transfers to VDN 70200 (sales)
16. Mock routes through CM10 → sales-queue → Agent Alice connected
17. Agent Console shows active call on Alice's card
```

## Docker Compose

```yaml
# simulator/docker-compose.yml
services:
  pstn-simulator:
    build:
      context: ./src/PstnSimulator
    ports:
      - "5200:8080"
    environment:
      - AcsMode=Mock
      - IvrEndpoint=http://host.docker.internal:7071
```

### Shared Network with IVR

To run both IVR and simulator on the same Docker network:

```bash
# Create the shared network
docker network create ivr-network

# In the IVR docker-compose.yml, add:
# networks:
#   ivr-network:
#     external: true

# In the simulator docker-compose.yml, set:
# networks:
#   ivr-network:
#     external: true

# Update IVR environment:
# GRAPH_API_ENDPOINT=http://pstn-simulator:8080/graph/v1.0

# Update simulator environment:
# IvrEndpoint=http://ivr-functions:80
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| IVR can't reach mock Graph API | Ensure both containers are on the same Docker network |
| "No trunk capacity" | Increase `Trunks[].Capacity` in appsettings.json |
| DTMF keys disabled | Call must be in `IvrRecognizing` state with `recognizeType=dtmf` |
| Speech input disabled | IVR must use speech recognition (not DTMF) |
| "VDN not found" | Check `Dids[].VdnNumber` matches a `Vdns[].Number` in config |
| Agent not assigned | Ensure at least one agent in the queue has `State=Available` |
