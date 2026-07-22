# Terminology & Glossary

Reference definitions for telephony, Microsoft Teams, and Azure terms used throughout the E911 IVR system.

---

## Telephony Terms

### ANI — Automatic Number Identification
The caller's phone number as delivered by the carrier network. In E911 systems, ANI is the primary key used to identify who is calling, look up their record in the ANI database, and drive routing decisions (VIP bypass, blocking, custom menus). Equivalent to caller ID but at the carrier/network level rather than the handset.

### ALI — Automatic Location Identification
The physical address or location associated with a phone number, maintained in a separate database linked to ANI. In E911 contexts, ALI provides the dispatcher (or the IVR) with the caller's location without the caller needing to state it — critical for emergency response. ALI records include street address, building, floor, GPS coordinates, service area, and region.

### PSTN — Public Switched Telephone Network
The global circuit-switched telephone network that carries traditional voice calls. Includes landline infrastructure, cellular carrier networks, and the interconnections between them. In this system, PSTN calls arrive via Direct Routing through a Session Border Controller into Microsoft Teams Phone System.

### DID — Direct Inward Dialing
A telephone number assigned to a specific endpoint or service. In IVR systems, each DID can be configured to route callers into a different menu tree, enabling a single IVR deployment to serve multiple lines (e.g., a main E911 line, an after-hours line, a test line). Also called a "phone number," "DDI" (in the UK), or simply "the number."

### Direct Routing
A Microsoft Teams Phone System feature that connects an organization's existing PSTN carrier to Teams via a customer-managed Session Border Controller (SBC). Instead of purchasing phone numbers from Microsoft, the organization keeps its existing numbers and carrier relationship while routing calls through Teams. This is the primary telephony architecture for this IVR system.

### SBC — Session Border Controller
A network device that sits at the boundary between the organization's IP network (or the PSTN carrier) and Microsoft Teams. It terminates SIP trunks, handles media transcoding, enforces security policies, and provides the gateway through which Direct Routing calls enter Teams. Certified SBCs for Teams include AudioCodes, Ribbon, Oracle, and Cisco.

### SIP — Session Initiation Protocol
The signaling protocol used to establish, manage, and terminate voice and video calls over IP networks. SIP is the language spoken between SBCs, PBXs, and cloud calling platforms. A SIP INVITE starts a call; a SIP BYE ends it.

### SIP URI
A SIP address that identifies a participant in a SIP call, formatted as `sip:user@domain` (e.g., `sip:+15551234567@sbc.contoso.com`). For Direct Routing calls, caller and called numbers arrive at the IVR as SIP URIs rather than plain E.164 numbers and must be parsed to extract the phone number.

### E.164
The international standard phone number format: `+` followed by country code and subscriber number, no spaces or dashes (e.g., `+17035550911`). All phone numbers stored in Cosmos DB and passed between system components use E.164 format.

### DTMF — Dual-Tone Multi-Frequency
The tones generated when a caller presses a key on a touchtone phone keypad. Each key produces a unique combination of two audio frequencies (e.g., pressing "1" sends 697 Hz + 1209 Hz). The IVR listens for these tones to capture caller input. DTMF is the primary input mechanism for traditional IVR navigation. Also referred to as "touch-tone."

### IVR — Interactive Voice Response
An automated telephony system that interacts with callers through pre-recorded or text-to-speech audio prompts and collects input via DTMF keypresses or spoken speech. The IVR navigates callers through a menu tree, identifies their need, and routes them to the correct destination — all without human agent involvement.

### VDN — Vector Directory Number
An Avaya Call Manager concept. A VDN is a virtual extension that points to a call vector (a script defining routing logic). In this system, when the IVR completes its interaction, it can transfer the call to a CM10 VDN, which then handles ACD (Automatic Call Distribution) routing to live agents or queues.

### ACD — Automatic Call Distribution
A telephony system that automatically routes incoming calls to available agents based on defined rules (skills, priority, wait time, etc.). In this architecture, the IVR handles the initial interaction and then hands off to an ACD system (typically Avaya CM10) for live-agent distribution.

### PBX — Private Branch Exchange
An organization's private telephone switching system, which manages internal extensions and connects them to the PSTN. Avaya Communication Manager (CM10) is a PBX. The IVR system integrates with the customer's PBX for VDN transfers and ACD routing.

### Trunk / SIP Trunk
A virtual telephone line that carries multiple simultaneous calls over an IP network between two endpoints (e.g., between a carrier and an SBC, or between an SBC and Teams). A SIP trunk replaces traditional physical phone lines (T1/PRI circuits).

### CDR — Call Detail Record
A log record capturing metadata about a single telephone call: caller number, called number, start time, end time, duration, and disposition. The IVR stores an enhanced CDR in Cosmos DB (`CallLogs` container) that includes the full menu navigation path, transcript, detected intent, and external system results.

### Disposition
How a call ended. Common values in this system: `Completed`, `TransferredToExternal`, `CallerHangup`, `Abandoned`, `SystemError`, `Voicemail`. Used in reporting and audit trail.

---

## Microsoft Teams & Azure Terms

### Microsoft Teams Phone System
Microsoft's cloud-based Private Branch Exchange (PBX) built into Teams. It provides PSTN calling capabilities through either Microsoft Calling Plans (numbers purchased from Microsoft) or Direct Routing (numbers from the organization's own carrier). For this IVR, Teams Phone System is the entry point for all inbound calls.

### Microsoft Graph Calling API
A subset of the Microsoft Graph REST API that provides programmatic control of Teams calls. The IVR uses Graph to answer calls (`PATCH /communications/calls/{id}`), play audio prompts (`POST /playPrompt`), subscribe to DTMF tones (`POST /subscribeToTone`), record speech (`POST /record`), and transfer calls (`POST /transfer`). All real-time call control flows through this API.

### commsNotification
The JSON payload Microsoft Graph sends to the IVR bot endpoint (`POST /api/bot-messages`) when a call event occurs. Every lifecycle event — incoming call, call established, tone received, prompt completed, call terminated — arrives as a `commsNotification`. The IVR function parses the notification and dispatches to the correct handler.

### Bot Framework / Bot Service
Microsoft's platform for building conversational bots that integrate with Teams, Outlook, and other channels. The IVR is registered as a Bot Framework bot, which provides the authentication layer (App ID / App Password) and the channel routing that allows Microsoft Graph to deliver call notifications to the Function App.

### App Registration (Azure AD / Entra ID)
A configuration entry in Microsoft Entra ID (formerly Azure Active Directory) that gives an application an identity. The IVR bot has an App Registration with a Client ID and Client Secret, which are used to authenticate calls between the Function App and the Microsoft Graph Calling API.

### Azure Functions
Microsoft's serverless compute platform. Code runs in response to triggers (HTTP requests, timers, queue messages) without managing servers. The IVR engine is an Azure Functions app (.NET 8, isolated worker model) where all call handling logic runs. The primary function (`TeamsCallBot`) is triggered by HTTP POST from Microsoft Graph.

### Azure Cosmos DB
Microsoft's globally distributed, multi-model NoSQL database. The IVR uses Cosmos DB (serverless tier) as its primary data store for all configuration and call history: menus, prompts, ANI/ALI records, call logs, system settings, team routing configs, and phone number configurations. Serverless tier charges per request unit (RU) consumed.

### Azure Blob Storage
Microsoft's object storage service for unstructured data. The IVR uses Blob Storage to cache synthesized TTS audio files. When a prompt is generated for the first time, the WAV file is stored in the `ivr-prompts` container. Subsequent calls retrieve it from Blob Storage instead of re-synthesizing, saving cost and latency. SAS URLs (time-limited signed URLs) are used to give Microsoft Graph temporary read access to the audio files.

### SAS URL — Shared Access Signature URL
A time-limited, cryptographically signed URL that grants read (or other) access to a specific Azure Blob Storage resource without requiring the caller to have storage credentials. The IVR generates a SAS URL for each TTS audio file and passes it to Microsoft Graph's `playPrompt` API. SAS URLs expire after a configured duration (typically 1 hour).

### Azure Cognitive Services — Speech
Microsoft's AI service for converting text to speech (TTS) and speech to text (STT). The IVR uses this service to synthesize prompt audio from TTS text stored in Cosmos DB, using neural voices for natural-sounding output.

### TTS — Text-to-Speech
The process of converting written text into spoken audio. The IVR stores prompt text (e.g., `"Press 1 for Emergency Dispatch"`) in Cosmos DB and synthesizes it to a WAV file using Azure Cognitive Services Speech at call time. The audio is cached in Blob Storage to avoid repeated synthesis costs.

### STT — Speech-to-Text
The process of converting spoken audio into written text (a transcript). The IVR uses Teams' built-in speech capture (`record` operation) to capture the caller's spoken words, then passes the transcript to Azure OpenAI for intent classification. Enables natural language IVR navigation without requiring callers to press keys.

### SSML — Speech Synthesis Markup Language
An XML-based markup language that gives precise control over TTS output — pausing, emphasis, pronunciation, speaking rate, and prosody. The IVR supports SSML prompts for advanced scenarios such as inserting a caller's name into a greeting or varying emphasis on important words.

### Azure OpenAI
Microsoft's managed deployment of OpenAI's large language models (GPT-4o, GPT-4.1, etc.) within Azure infrastructure. The IVR uses Azure OpenAI in two pipelines: (1) **Intent Classification** — classify a caller's spoken words into one of N configured routing destinations; (2) **Data Extraction** — extract structured fields (building, action, system type) from a transcript for submission to external REST APIs.

### Azure App Service
Microsoft's fully managed platform for hosting web applications. The Admin Portal (Blazor Server) and PSTN Simulator (Blazor Server) are both hosted on Azure App Service (Windows or Linux). App Service handles SSL, scaling, deployment slots, and managed identity without requiring VM management.

### Azure Application Insights
Microsoft's application performance monitoring (APM) service. All IVR components (Functions, Admin Portal, Simulator) send telemetry — traces, exceptions, dependency calls, custom events — to Application Insights. Used for real-time monitoring, failure diagnosis, call volume dashboards, and KQL-based alerting.

### Managed Identity
An Azure feature that gives a service (Function App, App Service) an automatically managed Azure AD identity, eliminating the need to store credentials in configuration. The IVR uses managed identity where possible (e.g., to access Cosmos DB and Blob Storage without connection string secrets).

### Azure Event Grid
Microsoft's event routing service that delivers events from Azure resources to subscriber endpoints. In the original ACS architecture, Event Grid delivered `IncomingCall` events to the IVR. In the current Teams integration, Event Grid is not used for call routing — Microsoft Graph delivers call notifications directly via HTTP POST.

### Bicep
Microsoft's domain-specific language (DSL) for deploying Azure infrastructure as code. Bicep compiles to ARM (Azure Resource Manager) JSON templates. All IVR Azure resources (Cosmos DB, Storage, Function App, App Service, Cognitive Services, OpenAI) are defined in Bicep files under `infra/`.

---

## IVR System-Specific Terms

### Menu Tree
The hierarchical structure of IVR menus that defines the call flow. Each node in the tree is an `IvrMenu` with a prompt, timeout, max retries, and a list of options. The root menu is the first menu callers hear. Sub-menus branch off for further navigation.

### Root Menu
The entry-point menu that callers reach when they dial a DID. Each DID can have its own root menu (configured in `PhoneNumberConfig`); otherwise, the system-wide root menu applies.

### Menu Option / Action
A mapping from a DTMF key (or speech keyword) to an action. Actions include: navigate to a sub-menu, transfer to a number or VDN, play a prompt, hang up, fire a webhook, or submit data to an external system.

### ANI Record
A Cosmos DB document in the `AniRecords` container that maps a phone number to caller metadata: name, account number, caller type, priority, VIP flag, blocked flag, preferred language, and custom routing menu ID.

### ALI Record
A Cosmos DB document in the `AliRecords` container that maps a phone number to a physical location: street address, city, state, zip, GPS coordinates, location type, service area, and timezone.

### Call Log
A Cosmos DB document in the `CallLogs` container that records the complete history of a single call: caller/called numbers, ANI/ALI snapshot, menu navigation path, transcript, detected intent, external system results, start/end times, duration, status, and disposition. 90-day TTL.

### Prompt
A Cosmos DB document in the `Prompts` container defining a single audio message. Can be TTS text, a pre-recorded audio file URL, or SSML markup. Prompts are referenced by ID from menus and actions.

### Team Routing Config
A Cosmos DB document in the `TeamRouting` container that defines an AI-routable destination: team name, description, intent keywords, transfer number, queue name, and priority. Used by `TranscriptRoutingService` to match a caller's spoken words to the correct team.

### VIP Routing
When a caller's ANI record has `IsVip = true` and a `CustomRoutingMenuId` set, the IVR bypasses the normal menu tree and routes directly to the VIP-specific menu. Used for high-priority callers, executive lines, or facilities with specialized handling.

### Business Hours Config
A Cosmos DB document defining when the IVR is "open": a 7-day schedule with open/close times, a timezone, an after-hours menu ID, a holiday menu ID, and a list of holiday dates. The IVR evaluates this at the start of every call.
