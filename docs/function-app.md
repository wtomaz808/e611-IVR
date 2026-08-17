# Function App - IVR Call Handling Engine

## Overview

The **IVR Function App** is the automated call handling engine that powers the E911 IVR system. It operates entirely in the background with **zero human interaction** - all operations are triggered automatically by Azure services in response to incoming phone calls.

**Key Characteristics:**
- **Technology**: Azure Functions v4 (.NET 8, isolated worker model)
- **Runtime**: Serverless, event-driven architecture
- **Interaction Model**: Machine-to-machine only (no user interface)
- **Deployment**: Azure Government or Azure Commercial cloud

## Architecture Role

```
┌─────────────────────────────────────────────────────────────┐
│  Phone Call Flow (Fully Automated)                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. 📞 Caller dials phone number (Teams Direct Routing)     │
│            ↓                                                 │
│  2. 📡 Microsoft Teams Phone System receives call           │
│            ↓                                                 │
│  3. 🔔 Graph commsNotification → TeamsCallBot Function      │
│            ↓                                                 │
│  4. 🤖 Function App automatically:                          │
│       • Looks up ANI/ALI (caller identification/location)   │
│       • Checks if caller is blocked                         │
│       • Answers call via Microsoft Graph Calling API        │
│       • Plays greeting prompt (TTS via Cognitive Services)  │
│            ↓                                                 │
│  5. 👤 Caller provides input (DTMF or voice)                │
│            ↓                                                 │
│  6. 🔔 Graph commsNotification → TeamsCallBot Function      │
│            ↓                                                 │
│  7. 🤖 Function App processes input:                        │
│       • Navigates menu hierarchy                            │
│       • Plays next prompt                                   │
│       • OR transfers to Teams VDN / external number         │
│       • OR sends data to external system                    │
│       • Logs all activity                                   │
│            ↓                                                 │
│  8. 🔁 Steps 5-7 repeat until call completion               │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Function Endpoints

The Function App exposes a single HTTP-triggered function that receives all Microsoft Graph calling notifications:

### TeamsCallBot

**Endpoint**: `POST /api/bot-messages`  
**Trigger Source**: Microsoft Graph Calling API (commsNotifications)  
**Notification Types**:
- `microsoft.graph.call` (incoming call)
- `microsoft.graph.callRecord`

#### Purpose
Single entry point for all Teams calling events. Microsoft Graph sends a `commsNotification` POST to this endpoint for every call lifecycle event.

#### Process Flow

```csharp
1. Receive commsNotification from Microsoft Graph
2. Authenticate: validate MicrosoftAppId / MicrosoftAppPassword
3. Parse notification type:

   ┌─ Incoming Call ────────────────────────────┐
   │  • Extract caller and called numbers       │
   │  • Perform ANI/ALI lookup                  │
   │  • Security check (blocked caller)         │
   │  • Create CallLog in Cosmos DB             │
   │  • Answer call via Graph PATCH /calls/{id} │
   │  • Determine root menu (per-DID, VIP,      │
   │    business hours, conditions)             │
   └────────────────────────────────────────────┘

   ┌─ Call Established ─────────────────────────┐
   │  • Start menu navigation                   │
   │  • Play welcome prompt (TTS SAS URL)       │
   │  • Begin DTMF/speech recognition           │
   └────────────────────────────────────────────┘

   ┌─ Tone Received (DTMF) ─────────────────────┐
   │  • Match tone to MenuOption.DtmfKey        │
   │  • Execute action (navigate / transfer /   │
   │    webhook / external system)              │
   │  • Update call log with menu path          │
   └────────────────────────────────────────────┘

   ┌─ Recording / Speech ───────────────────────┐
   │  • Extract transcript from clientContext   │
   │  • Classify with Azure OpenAI              │
   │  • Route to matched team                   │
   └────────────────────────────────────────────┘

   ┌─ Call Terminated ──────────────────────────┐
   │  • Update call log (end time, disposition) │
   │  • Clean up in-memory call state           │
   └────────────────────────────────────────────┘
```

#### Code Location
`src/IVR.Functions/Functions/TeamsCallBot.cs`

#### Key Features
- **Single endpoint**: All call events arrive at `/api/bot-messages`
- **Multi-level menu navigation**: Unlimited menu depth
- **DTMF input processing**: Standard touchtone keypad
- **Speech recognition**: Natural language input via Teams record operation
- **AI-powered routing**: Azure OpenAI intent classification (optional)
- **Retry logic**: Configurable timeout and invalid input handling
- **External integrations**: HTTP webhooks to fire alarm panels, CAD systems, etc.
- **Teams VDN transfer**: Transfer calls to VDN addresses via Microsoft Graph
- **Data extraction**: Collect caller input and push to external systems

---

## Supporting Services

The Function App coordinates with several internal services to provide complete IVR functionality:

### CallFlowEngine Service
**File**: `src/IVR.Functions/Services/CallFlowEngine.cs`

**Responsibilities:**
- Build call context with ANI/ALI data
- Resolve which menu to display based on caller attributes
- Evaluate conditional routing rules
- Check business hours configuration
- Apply VIP and custom routing logic

**Key Methods:**
```csharp
Task<CallContext> BuildCallContextAsync(string callerNumber, string calledNumber)
Task<IvrMenu> ResolveMenuAsync(string callerNumber, string? calledNumber, string? currentMenuId)
Task<IvrMenu?> EvaluateConditionsAsync(IvrMenu menu, AniRecord? ani, AliRecord? ali)
```

---

### AniAliService
**File**: `src/IVR.Functions/Services/AniAliService.cs`

**Responsibilities:**
- Look up caller information by phone number
- Normalize phone numbers to E.164 format
- Extract phone numbers from SIP URIs
- Provide caller metadata (name, account, location, VIP status)

**Key Methods:**
```csharp
Task<(AniRecord? ani, AliRecord? ali)> LookupCallerAsync(string phoneNumber)
string NormalizePhoneNumber(string phoneNumber)
string ExtractPhoneFromUri(string uri)
```

**ANI Data Includes:**
- Caller name
- Account number
- VIP flag
- Blocked flag
- Preferred language
- Custom routing menu ID

**ALI Data Includes:**
- Street address
- City, state, ZIP
- Building/floor/room
- GPS coordinates
- Special instructions

---

### PromptService
**File**: `src/IVR.Functions/Services/PromptService.cs`

**Responsibilities:**
- Retrieve prompt configurations from Cosmos DB
- Build FileSource or TextSource for Call Automation
- Handle both pre-recorded audio and text-to-speech
- Manage audio file URLs from Blob Storage

**Key Methods:**
```csharp
Task<PlaySource?> GetPromptSourceAsync(string promptId)
Task PlayPromptAsync(CallConnection callConnection, string promptId, string targetParticipant)
```

**Prompt Types:**
1. **Pre-recorded audio**: `.wav` files from Azure Blob Storage
2. **Text-to-Speech**: Azure Cognitive Services Speech synthesis
3. **SSML**: Advanced speech markup for dynamic content

---

### TranscriptRoutingService
**File**: `src/IVR.Functions/Services/TranscriptRoutingService.cs`

**Responsibilities:**
- Send speech transcripts to Azure OpenAI
- Classify caller intent using GPT-4
- Match intent to Team Routing configurations
- Provide confidence scores for routing decisions
- Handle fallback when intent is unclear

**AI Routing Flow:**
```
1. Caller says: "I need help with a fire alarm issue"
2. Speech-to-text transcribes the utterance
3. TranscriptRoutingService sends to Azure OpenAI:
   - System prompt with team descriptions
   - User transcript
4. OpenAI returns: { team: "Fire Safety Team", confidence: 0.95 }
5. CallbackHandler transfers to that Teams queue
```

**Key Methods:**
```csharp
Task<(string? teamName, double confidence)> ClassifyTranscriptAsync(
    string transcript, 
    List<TeamRoutingConfig> teams)
```

---

### ExternalSystemIntegrationService
**File**: `src/IVR.Functions/Services/ExternalSystemIntegrationService.cs`

**Responsibilities:**
- Send HTTP requests to external APIs
- Extract data from call context using JSONPath
- Map extracted data to external system fields
- Handle authentication (Basic, Bearer, API Key)
- Retry logic for failed integrations
- Log all external system interactions

**Integration Flow:**
```
1. Caller presses "9" for emergency
2. Menu action: ExternalSystemIntegration
3. Service extracts data:
   - Caller location from ALI
   - Caller phone from ANI
   - Current date/time
4. POST to fire alarm panel API:
   {
     "location": "Building A, Floor 3, Room 301",
     "phoneNumber": "+15551234567",
     "timestamp": "2026-03-02T14:30:00Z"
   }
5. Log result in call log
```

**Key Methods:**
```csharp
Task<bool> SendAsync(string configId, CallContext context)
Task<Dictionary<string, object>> ExtractDataAsync(
    List<DataExtractionConfig> extractors, 
    CallContext context)
```

---

## Configuration Data Sources

The Function App reads configuration from multiple sources:

### Cosmos DB Containers

| Container | Data | Used For |
|-----------|------|----------|
| **AniRecords** | Caller identification | Lookup caller name, VIP status, blocking |
| **AliRecords** | Caller location | Emergency location, address details |
| **Menus** | IVR menu definitions | Menu hierarchy, options, actions |
| **Prompts** | Audio prompts | Greetings, instructions, error messages |
| **CallLogs** | Call history (90-day TTL) | Audit trail, analytics |
| **Config** | System settings | Business hours, global defaults |
| **TeamRouting** | AI routing configs | Team descriptions for intent classification |
| **ExternalSystems** | Integration endpoints | Fire panels, CAD systems, work orders |
| **DataExtraction** | Field mapping rules | Extract call data for external APIs |
| **PhoneNumbers** | DID routing | Per-number menu assignments |

### Environment Variables / App Settings

| Setting | Purpose |
|---------|---------|
| `MicrosoftAppId` | Teams Bot Application (Client) ID |
| `MicrosoftAppPassword` | Teams Bot client secret |
| `MicrosoftAppTenantId` | Azure AD tenant ID |
| `GraphApiEndpoint` | Microsoft Graph endpoint (real or simulator) |
| `ChannelService` | Bot Framework channel service URL (Gov: `https://botframework.azure.us`) |
| `CosmosDbConnectionString` | Cosmos DB access. Deployed as a Key Vault reference (`@Microsoft.KeyVault(SecretUri=...)`), resolved at runtime via the Function App's system-assigned identity. |
| `StorageConnectionString` | Blob Storage for audio files |
| `CognitiveServicesEndpoint` | Text-to-speech synthesis |
| `CognitiveServicesKey` | Cognitive Services authentication key |
| `AzureOpenAIEndpoint` | GPT-4 for intent classification |
| `AzureOpenAIKey` | OpenAI authentication |
| `AzureOpenAIDeploymentName` | Model deployment ID |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Telemetry and monitoring |

---

## Advanced Features

### Speech Recognition & AI Routing

When `EnableSpeechRecognition` is enabled on a menu:

1. **Prompt**: "Please briefly describe your issue."
2. **Listen**: Capture up to 30 seconds of speech
3. **Transcribe**: Azure Cognitive Services Speech-to-Text
4. **Classify**: Send transcript to Azure OpenAI with team descriptions
5. **Route**: 
   - High confidence (>0.7) → Transfer to matched team
   - Low confidence (<0.7) → Execute fallback action (e.g., main menu)

**Example Prompt to GPT-4:**
```
You are an intelligent call routing assistant. Based on the caller's 
description, determine which team should handle the call.

Available Teams:
- Fire Safety Team: Fire alarms, sprinklers, smoke detectors, evacuation
- Facilities Team: HVAC, plumbing, electrical, maintenance
- Security Team: Access control, cameras, incidents, emergencies

Caller said: "The fire alarm is going off on the third floor"

Respond with JSON: {"team": "Fire Safety Team", "confidence": 0.95}
```

### Conditional Menu Routing

Menus support dynamic routing based on caller attributes:

```json
{
  "conditions": [
    {
      "field": "ani.isVip",
      "operator": "Equals",
      "value": "true",
      "targetMenuId": "vip-fast-track-menu"
    },
    {
      "field": "ali.building",
      "operator": "StartsWith",
      "value": "Hospital",
      "targetMenuId": "medical-emergency-menu"
    }
  ]
}
```

### Data Extraction & External Systems

Extract data from the call context and send to external APIs:

**Data Extraction Config:**
```json
{
  "extractors": [
    {
      "fieldName": "callerPhone",
      "sourceType": "CallContext",
      "jsonPath": "$.callerNumber"
    },
    {
      "fieldName": "location",
      "sourceType": "CallContext",
      "jsonPath": "$.aliData.fullAddress"
    },
    {
      "fieldName": "timestamp",
      "sourceType": "CurrentDateTime",
      "format": "yyyy-MM-ddTHH:mm:ssZ"
    }
  ]
}
```

**External System Config:**
```json
{
  "systemName": "Fire Alarm Panel",
  "endpoint": "https://fire-panel.contoso.com/api/alarms",
  "method": "POST",
  "authenticationType": "ApiKey",
  "authHeaderName": "X-API-Key",
  "authValue": "secret-key-here",
  "dataExtractionConfigId": "fire-alarm-data-extraction"
}
```

### Call Logging & Analytics

Every call generates a detailed log record:

```json
{
  "callId": "guid",
  "callerNumber": "+15551234567",
  "calledNumber": "+18005551212",
  "startTime": "2026-03-02T14:30:00Z",
  "endTime": "2026-03-02T14:35:30Z",
  "duration": 330,
  "status": "Completed",
  "aniData": { "callerName": "John Doe", "isVip": true },
  "aliData": { "building": "A", "floor": 3, "room": "301" },
  "menusVisited": ["main-menu", "facilities-menu", "hvac-submenu"],
  "inputReceived": ["1", "2"],
  "externalSystemCalls": [
    {
      "systemName": "Work Order API",
      "timestamp": "2026-03-02T14:33:00Z",
      "success": true,
      "httpStatus": 201
    }
  ],
  "finalAction": "TransferredToTeamsQueue",
  "teamsQueueId": "facilities-team-queue"
}
```

Logs include TTL (90 days) for automatic cleanup.

---

## Deployment

### Azure Resources Required

- **Azure Functions** (Consumption or Premium plan)
- **Microsoft Teams Phone System** (Direct Routing configured with SBC)
- **Azure Cosmos DB** (serverless recommended for dev/test)
- **Azure Blob Storage** (for audio prompt files)
- **Azure Cognitive Services** (Speech TTS)
- **Azure OpenAI** (optional, for AI routing)
- **Application Insights** (monitoring and diagnostics)

### Deployment Methods

1. **Azure Developer CLI (azd)**
   ```bash
   cd infra
   azd up
   ```

2. **Azure Functions Core Tools**
   ```bash
   cd src/IVR.Functions
   func azure functionapp publish <function-app-name> --dotnet-isolated
   ```

3. **CI/CD Pipeline** (GitHub Actions, Azure DevOps)
   - Build .NET project
   - Run tests
   - Deploy via Azure CLI or Bicep

### Azure Government Deployment

For Azure Government cloud:
- Use `AzureUSGovernment` endpoints
- Use `https://login.microsoftonline.us` for authentication
- Use `https://botframework.azure.us` as the channel service
- Ensure all services are available in Gov regions (some features limited)

**Note**: Azure OpenAI may have limited model availability in Azure Government. The system falls back to DTMF-only navigation if OpenAI is not configured.

---

## Monitoring & Troubleshooting

### Application Insights Integration

All functions log to Application Insights:

**Key Metrics:**
- Function execution count and duration
- Dependency calls (Cosmos DB, Blob Storage, External APIs)
- Exception rates
- Custom events (call start, call end, menu navigation)

**Useful Queries (KQL):**

**Call Volume by Hour:**
```kql
customEvents
| where name == "CallStarted"
| summarize CallCount = count() by bin(timestamp, 1h)
| render timechart
```

**Average Call Duration:**
```kql
customEvents
| where name == "CallEnded"
| extend duration = todouble(customDimensions.durationSeconds)
| summarize AvgDuration = avg(duration), MaxDuration = max(duration)
```

**Failed Recognitions:**
```kql
customEvents
| where name == "RecognizeFailed"
| summarize FailureCount = count() by tostring(customDimensions.reason)
```

### Common Issues

**1. Calls not being answered**
- Verify `MicrosoftAppId` and `MicrosoftAppPassword` are set correctly
- Confirm `GraphApiEndpoint` points to the correct endpoint (real or simulator)
- Confirm Function App is running (not stopped)
- Review logs for exceptions in TeamsCallBot

**2. DTMF not recognized**
- Ensure `RecognizeCompleted` events are received
- Check menu configuration has valid DTMF options
- Verify timeout settings (default: 10 seconds)

**3. Speech recognition not working**
- Confirm `CognitiveServicesEndpoint` is configured
- Check Cognitive Services resource is provisioned
- Review Speech API quota limits

**4. External system integration failures**
- Verify endpoint URLs are accessible from Azure
- Check authentication credentials are correct
- Review HTTP response codes in call logs
- Confirm network security allows outbound HTTPS

**5. Menu logic not executing**
- Validate menu JSON structure in Cosmos DB
- Check for circular menu references
- Verify prompt IDs exist in Prompts container
- Review condition logic (field names, operators)

---

## Security Considerations

### Authentication & Authorization

- **Inbound webhooks**: Anonymous (validated via Event Grid subscription or ACS signatures)
- **Outbound calls**: Managed Identity or connection strings
- **Cosmos DB**: Account keys or Managed Identity
- **Blob Storage**: Account keys or Managed Identity
- **Azure OpenAI**: API keys or Managed Identity

**Best Practice**: Use Managed Identity for all Azure service connections in production.

### Data Privacy

- **Call recordings**: Not stored by default (enable via ACS configuration)
- **Transcripts**: Stored in call logs with 90-day TTL
- **ANI/ALI data**: Sensitive - restrict Cosmos DB access
- **External API credentials**: Store in Azure Key Vault, reference via app settings

### Network Security

- **Function App**: Can be deployed in VNet for private networking
- **Cosmos DB**: Enable firewall rules to restrict access
- **Blob Storage**: Use SAS tokens with expiration for prompt URLs
- **External integrations**: Whitelist Function App IP if required

---

## Performance & Scaling

### Serverless Benefits

- **Auto-scaling**: Handles 1 to 10,000+ concurrent calls
- **Pay-per-execution**: No idle costs
- **Global distribution**: Deploy close to ACS regions

### Optimization Tips

1. **Use Premium Plan** for:
   - Reduced cold starts
   - VNet integration
   - Larger instance sizes
   - Always-on availability

2. **Cache menu configurations** in-memory to reduce Cosmos DB reads

3. **Pre-warm instances** using scheduled pings or Application Insights availability tests

4. **Optimize Cosmos DB queries**:
   - Use partition keys effectively
   - Index frequently queried fields
   - Limit SELECT results

5. **Audio prompt optimization**:
   - Use `.wav` format (uncompressed PCM)
   - Sample rate: 16kHz or 24kHz
   - Store in Hot tier Blob Storage for low latency

---

## Development & Testing

### Local Development

Run the Function App locally using Azure Functions Core Tools:

```bash
cd src/IVR.Functions
func start
```

**Mock Mode**: Use the PSTN Simulator for local testing without real phone numbers:
```bash
cd simulator
docker-compose up
```

The simulator provides:
- Mock ACS bridge (no real phone calls)
- Agent console to simulate phone behavior
- Visual call flow monitoring

### Unit Testing

Test individual services in isolation:

```csharp
[Fact]
public async Task AniAliService_LookupCaller_ReturnsCorrectData()
{
    // Arrange
    var mockCosmosDb = new Mock<ICosmosDbService>();
    var service = new AniAliService(mockCosmosDb.Object);
    
    // Act
    var (ani, ali) = await service.LookupCallerAsync("+15551234567");
    
    // Assert
    Assert.NotNull(ani);
    Assert.Equal("John Doe", ani.CallerName);
}
```

### Integration Testing

Test end-to-end call flows:

1. Deploy to dev environment
2. Configure test phone number in ACS
3. Use test ANI/ALI records
4. Place test call and verify:
   - Call is answered
   - Prompts play correctly
   - DTMF navigation works
   - Call logs are created
   - External systems receive data (use webhook.site for testing)

---

## Further Reading

- [Architecture Documentation](architecture.md) - Complete system design
- [Admin Portal Guide](admin-portal.md) - Configure menus and prompts
- [Microsoft Graph Calling API Docs](https://learn.microsoft.com/graph/api/resources/communications-api-overview)
- [Azure Functions Best Practices](https://learn.microsoft.com/azure/azure-functions/functions-best-practices)
