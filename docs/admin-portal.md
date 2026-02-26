# Admin Portal User Guide

The IVR Admin Portal is a Blazor Server web application for managing every aspect of the IVR system — menus, prompts, caller records, call logs, and system settings. It is protected by Azure AD authentication and requires the `IVR.Admin` role.

---

## Table of Contents

1. [Accessing the Portal](#1-accessing-the-portal)
2. [Dashboard](#2-dashboard)
3. [Call Flow Builder](#3-call-flow-builder)
4. [Prompt Management](#4-prompt-management)
5. [ANI/ALI Data Management](#5-aniali-data-management)
6. [Call Logs](#6-call-logs)
7. [Phone Numbers (PSTN)](#7-phone-numbers-pstn)
8. [Settings](#8-settings)
9. [Common Tasks](#9-common-tasks)

---

## 1. Accessing the Portal

### URL
- **Development**: `https://localhost:5001` (or configured port)
- **Production**: `https://{your-app-service-name}.azurewebsites.net`

### Authentication
The portal uses **Azure AD (Microsoft Entra ID)** for authentication:
1. Navigate to the portal URL.
2. You will be redirected to the Microsoft login page.
3. Sign in with your organizational account.
4. You must have the **IVR.Admin** role assigned in Azure AD to access the portal.

### Sign Out
Click the **Admin** dropdown in the top-right corner and select **Sign Out**.

### Navigation
The left sidebar provides navigation to all sections:

| Icon | Section | Description |
|---|---|---|
| Speedometer | **Dashboard** | Real-time call analytics and system status |
| Megaphone | **Prompts** | Create and manage TTS/audio prompts |
| Diagram | **Call Flows** | Visual IVR menu tree builder |
| Person | **ANI / ALI Data** | Caller identification and location records |
| Journal | **Call Logs** | Searchable call history |
| Clock | **Business Hours** | Business hours and holiday schedule |
| Gear | **Settings** | Global system configuration |

The top-right shows the system status indicator (Online/Offline).

---

## 2. Dashboard

**Route**: `/` (home page)

The dashboard provides a real-time overview of IVR system activity.

### Summary Cards

Four cards at the top display key metrics:

| Card | Description |
|---|---|
| **Today's Calls** | Total number of calls received today |
| **Avg Duration** | Average call duration today (mm:ss) |
| **Active Menus** | Number of active IVR menu nodes |
| **Status** | Current business hours status (Open / After Hours) |

### Call Disposition Chart

A horizontal bar chart showing the breakdown of call outcomes for the past 7 days:

| Disposition | Description |
|---|---|
| Completed | Call completed normally |
| TransferredToAgent | Transferred to a live agent |
| TransferredToQueue | Placed in a call queue |
| TransferredToExternal | Transferred to an external number |
| Voicemail | Sent to voicemail |
| Abandoned | Caller hung up before completion |
| CallerHangup | Caller disconnected |
| SystemError | System failure during call |

### Calls by Hour

A vertical bar histogram showing call volume by hour of day (today), useful for identifying peak hours.

### Recent Calls Table

The 10 most recent calls today, showing:
- **Time**: Call start time (HH:mm:ss)
- **Caller**: Phone number
- **Name**: Caller name from ANI data (or "—" if unknown)
- **Status**: Call status badge (color-coded)
- **Duration**: Call duration (mm:ss)
- **Disposition**: How the call ended

Click **View All** to go to the full Call Logs page.

---

## 3. Call Flow Builder

**Route**: `/call-flows`

The Call Flow Builder lets you create, edit, and organize the IVR menu tree — the sequence of prompts and options callers navigate through.

### Layout

The page is split into two panels:

- **Left panel (Menu Tree)**: Hierarchical list of all menus, with root menus at the top and child menus indented below. Each menu shows its name, active status badge, and root badge if applicable.
- **Right panel (Menu Detail)**: Shows the selected menu's full configuration and options.

### Creating a New Menu

1. Click **New Menu** in the top-right.
2. Fill in the form:
   - **Name**: Menu identifier (e.g., "Main Menu", "Billing Sub-Menu")
   - **Description**: Purpose of this menu
   - **Menu Type**: Select the type:
     | Type | Purpose |
     |---|---|
     | Standard | Normal DTMF menu |
     | Root | Entry point of the IVR (only one should be active) |
     | SubMenu | Child of another menu |
     | AfterHours | Played outside business hours |
     | Holiday | Played on holidays |
     | Emergency | Emergency routing |
     | VipRouting | Custom flow for VIP callers |
     | SpeechRouting | AI-powered speech input menu |
   - **Prompt**: Select the prompt to play when entering this menu
   - **Parent Menu**: Select a parent (for sub-menus)
   - **Timeout (seconds)**: How long to wait for input (default: 10)
   - **Max Retries**: Number of reprompts before fallback action (default: 3)
3. Click **Save**.

### Adding Menu Options

Each menu has a list of options that map inputs to actions:

1. Select a menu in the tree.
2. In the detail panel, click **Add Option**.
3. Configure the option:
   - **DTMF Key**: The digit(s) to press (e.g., "1", "2", "#")
   - **Label**: Description (e.g., "Billing Department")
   - **Speech Keywords**: Words that match this option in speech mode
   - **Action Type**: What happens when selected:

     | Action | Configuration |
     |---|---|
     | Navigate to Menu | Select a target sub-menu |
     | Transfer to Number | Enter the phone number (E.164 format) |
     | Transfer to Team | AI-routed transfer (speech mode) |
     | Play Prompt | Select a prompt to play |
     | Hang Up | Optionally select a goodbye prompt |
     | Repeat Menu | Replays the current menu |
     | Webhook | Enter the webhook URL |
     | Submit to External System | Select a Data Extraction Config |

4. Click **Save Option**.

### Configuring Speech Routing

To enable AI-powered speech routing on a menu:

1. Select or create a menu.
2. Set **Menu Type** to `SpeechRouting`.
3. Toggle **Enable Speech Recognition** on.
4. Set **Speech Routing Prompt**: The prompt that says "Please describe your issue" or similar.
5. Add **Team Routing Config IDs**: Select which teams can be matched.
6. Set **Speech Fallback Action**: What to do if AI can't classify the intent (e.g., transfer to operator).
7. Save the menu.

### Menu Conditions

Menus can have conditional routing rules that override normal flow:

| Condition Type | Matches On | Example |
|---|---|---|
| BusinessHours | Current time vs. schedule | Route to after-hours menu when closed |
| AniMatch | Caller phone number | Route specific callers to custom menus |
| AliRegion | Caller's geographic region | Route by region for local services |
| CallerType | ANI caller type field | Route business vs. residential callers |
| VipStatus | ANI VIP flag | Route VIP callers to priority menu |
| Holiday | Holiday calendar | Route to holiday menu |

### Setting the Root Menu

One menu must be marked as the **Root Menu** — this is the first menu callers hear. To set it:

1. Select the menu in the tree.
2. In the detail panel, check **Is Root Menu**.
3. Save.

Alternatively, set the root menu ID in **Settings > General > Root Menu ID**.

---

## 4. Prompt Management

**Route**: `/prompts`

### Search and Filter

The top of the page provides:
- **Search box**: Filter prompts by name or description
- **Type filter**: Show only TTS, Audio File, or SSML prompts
- **Status filter**: Show only Active or Inactive prompts

### Prompts Table

Lists all prompts with columns:

| Column | Description |
|---|---|
| Name | Prompt name |
| Type | TTS / AudioFile / SSML (with icon) |
| Language | Language code (e.g., en-US) |
| Voice | TTS voice name |
| Category | Organizational category |
| Status | Active/Inactive badge |
| Actions | Edit / Delete buttons |

### Creating a Prompt

1. Click **New Prompt**.
2. Fill in:
   - **Name** (required)
   - **Description**
   - **Type**: TTS, Audio File, or SSML
   - For TTS:
     - **TTS Text**: The text to speak
     - **Voice**: Select a neural voice
     - **Style**: Optional voice style
   - For Audio File:
     - Upload a WAV/MP3 file
   - For SSML:
     - **SSML Content**: Full SSML markup
   - **Language**: Select language
   - **Category**: Optional grouping
   - **Tags**: Optional searchable tags
3. Click **Save**.

### Editing a Prompt

1. Click the **Edit** button on any row.
2. Modify fields in the modal.
3. The version number auto-increments on save.
4. Click **Save**.

### Deactivating a Prompt

Rather than deleting, set a prompt to **Inactive**. This preserves references in call logs while preventing the prompt from being used in new menus.

---

## 5. ANI/ALI Data Management

**Route**: `/ani-ali`

This page manages automatic caller identification (ANI) and location identification (ALI) records.

### Tab Layout

The page has two tabs:

- **ANI Records**: Caller identity data
- **ALI Records**: Caller location data

### ANI Records Tab

#### Search
Type a phone number, name, or account number in the search box to filter records.

#### Table Columns

| Column | Description |
|---|---|
| Phone Number | E.164 format phone number |
| Name | Caller's name |
| Account | Account number |
| Type | Caller type (Residential, Business, Government, etc.) |
| Priority | Routing priority (higher = more important) |
| VIP | VIP status badge |
| Blocked | Blocked status badge |
| Actions | Edit / Delete |

#### Creating an ANI Record

1. Click **New ANI Record**.
2. Fill in:
   - **Phone Number** (required): E.164 format (e.g., `+15551234567`)
   - **Caller Name**: Name displayed in call logs and dashboard
   - **Account Number**: Customer account reference
   - **Caller Type**: Residential, Business, Government, Emergency, Internal
   - **Priority**: 0–100 (higher = higher priority)
   - **Language**: Preferred language (e.g., `en-US`, `es-US`)
   - **VIP Flag**: Check to enable VIP routing
   - **Blocked**: Check to reject all calls from this number
   - **Custom Routing Menu ID**: Menu ID for personalized call flows
   - **Metadata**: Key-value pairs for custom data
3. Click **Save**.

#### VIP Routing
When a caller has VIP enabled and a Custom Routing Menu ID is set, they bypass the normal menu tree and go directly to their personalized menu.

#### Blocking a Caller
Check the **Blocked** flag to automatically reject calls from this number. The call will be rejected immediately at the `IncomingCallHandler` stage — no call log is created and no prompts are played.

### ALI Records Tab

#### Table Columns

| Column | Description |
|---|---|
| Phone Number | E.164 format phone number |
| Address | Street, City, State, Zip |
| Location Type | Residential, Commercial, Mobile, VoIP, Payphone |
| Service Area | Service area name |
| Region | Geographic region |
| Timezone | IANA timezone |

#### Creating an ALI Record

1. Click **New ALI Record**.
2. Fill in:
   - **Phone Number** (required)
   - **Street Address**, **City**, **State**, **Zip Code**, **Country**
   - **Latitude** / **Longitude** (optional geo-coordinates)
   - **Location Type**: Residential, Commercial, Mobile, VoIP, Payphone
   - **Service Area**: Named service area
   - **Region**: Geographic region (used in conditional routing)
   - **Timezone**: IANA timezone (e.g., `America/New_York`)
3. Click **Save**.

---

## 6. Call Logs

**Route**: `/call-logs`

### Filters

At the top of the page:
- **Date Range**: From/To date pickers to narrow the time window
- **Status Filter**: Filter by call status (Ringing, InProgress, Completed, Failed, Abandoned, etc.)

### Call Logs Table

| Column | Description |
|---|---|
| Time | Call start time |
| Caller | Caller phone number |
| Called | Called phone number |
| Caller Name | Name from ANI data |
| Status | Color-coded status badge |
| Duration | Call duration (mm:ss) |
| Disposition | How the call ended |
| Intent | AI-detected intent (for speech-routed calls) |
| Team | Team the call was routed to |
| Actions | View detail button |

### Call Detail Drawer

Click a call row to open the detail drawer showing:

#### Call Information
- Call ID, correlation ID
- Caller/called numbers
- Start time, end time, duration
- Status and disposition

#### ANI Data (if available)
- Caller name, account number
- Caller type, VIP status
- Language preference

#### ALI Data (if available)
- Full address
- Location type, service area
- Coordinates, region, timezone

#### Menu Path
A step-by-step trace of every menu visited during the call:
```
Main Menu → [pressed 2] → Support Menu → [pressed 1] → Billing → [transferred]
```

Each step shows the menu name, input given, and timestamp.

#### Transcript (for speech-routed calls)
The caller's spoken words, detected intent, confidence score, and routed team.

#### External System Results (if applicable)
For calls that triggered external system submissions:
- System name and endpoint action
- Success/failure status
- HTTP status code
- Confirmation value (e.g., ticket ID)
- Timestamp

#### Extracted Data
Structured fields extracted from the transcript by AI (e.g., `action: "test"`, `location: "Building 7"`).

---

## 7. Phone Numbers (PSTN)

**Route**: `/phone-numbers`

Manage the inventory of phone numbers (DIDs) that route into the IVR. Each number can be an ACS-native number, a direct-routed number through an SBC, or a SIP trunk number.

### Phone Numbers Table

| Column | Description |
|---|---|
| Phone Number | E.164 formatted phone number |
| Label | Descriptive name (e.g., "Main Support Line") |
| Type | `NativeAcs`, `DirectRouting`, or `SipTrunk` |
| Root Menu | The IVR menu callers on this number enter |
| SBC | FQDN of the Session Border Controller (if applicable) |
| Active | Whether the number is receiving calls |

### Adding a Phone Number

1. Click **Add Phone Number**.
2. Enter the phone number in E.164 format (e.g., `+15551234567`).
3. Enter a descriptive label.
4. Select the **Number Type**:
   - `NativeAcs` — number purchased inside Azure Communication Services
   - `DirectRouting` — customer-owned number routed via SBC
   - `SipTrunk` — number from a third-party SIP trunk provider
5. Select the **Root Menu** this number should drop callers into. If left blank, the system-wide root menu is used.
6. (Optional) Select per-number **Business Hours** and **Welcome Prompt** overrides.
7. For direct routing or SIP trunk: enter the **SBC FQDN** and **Port**.
8. Click **Save**.

### Called-Number Aliases

If a carrier or SBC rewrites the called number (e.g., stripping the `+1` prefix), add the alternate format to the **Called Number Aliases** list. The IVR engine will match any alias to this phone number config.

### Deactivating a Number

Toggle the **Active** switch to deactivate a number. Calls to deactivated numbers fall through to the system-wide root menu.

---

## 8. Settings

**Route**: `/settings`

### General Tab

| Setting | Description |
|---|---|
| Default Language | System-wide language (default: `en-US`) |
| Default TTS Voice | Default voice for TTS prompts |
| Max Call Duration | Maximum call duration in minutes (default: 60) |
| Record Calls | Enable/disable call recording |
| Enable Speech Recognition | System-wide speech recognition toggle |
| Root Menu ID | ID of the root IVR menu |
| Emergency Menu ID | ID of the emergency routing menu |
| Global Timeout (seconds) | Default input timeout (default: 15) |
| Global Max Retries | Default retry count (default: 3) |
| PSTN Mode | `NativeAcs`, `DirectRouting`, or `Hybrid` |
| Default SBC FQDN | Default Session Border Controller hostname (for direct routing) |
| Default SBC Port | Default SIP signaling port (default: 5067) |

#### Avaya CM10 Integration

| Setting | Description |
|---|---|
| Transfer to VDN on Disconnect | When enabled, caller disconnects are forwarded to the CM10 VDN for ACD routing |
| Default CM10 VDN | SIP URI or E.164 of the default Avaya CM10 VDN (e.g., `sip:70100@sbc.contoso.com` or `+18005550100`) |
| CM10 SBC FQDN | SBC FQDN for VDN transfers; falls back to Default SBC FQDN if empty |
| CM10 SBC Port | SBC SIP port for VDN transfers; falls back to Default SBC Port if empty |
| Transfer Prompt ID | Optional prompt to play before transferring to the CM10 VDN |

### Business Hours Tab

Configure when the office is open. Calls outside these hours are routed to the after-hours menu.

#### Schedule
A 7-day grid showing open/close times for each day:

| Day | Open | Open Time | Close Time |
|---|---|---|---|
| Monday | ✓ | 08:00 | 17:00 |
| Tuesday | ✓ | 08:00 | 17:00 |
| Wednesday | ✓ | 08:00 | 17:00 |
| Thursday | ✓ | 08:00 | 17:00 |
| Friday | ✓ | 08:00 | 17:00 |
| Saturday | ✗ | — | — |
| Sunday | ✗ | — | — |

#### Configuration
| Field | Description |
|---|---|
| Timezone | IANA timezone for schedule evaluation (e.g., `America/New_York`) |
| After-Hours Menu ID | Menu to display when office is closed |
| Holiday Menu ID | Menu to display on holidays |

### Holidays Tab

Manage a list of holiday dates when the office is closed:

| Field | Description |
|---|---|
| Date | The holiday date |
| Name | Holiday name (e.g., "New Year's Day", "Thanksgiving") |
| Use Holiday Menu | Whether to route to the holiday menu (vs. after-hours menu) |

To add a holiday:
1. Click **Add Holiday**.
2. Select the date.
3. Enter the holiday name.
4. Check **Use Holiday Menu** if you want to use the holiday-specific menu.
5. Click **Save**.

---

## 9. Common Tasks

### Task: Set Up a New IVR From Scratch

1. **Create Prompts**
   - Main menu welcome prompt
   - Each sub-menu prompt
   - Timeout/invalid input prompts
   - Goodbye prompt
   - After-hours/holiday prompts

2. **Create Menus**
   - Root menu (type: Root, flag: Is Root Menu)
   - Sub-menus for each department
   - After-hours menu
   - Holiday menu (optional)

3. **Wire Menu Options**
   - Root menu: options 1, 2, 3... pointing to sub-menus or transfers
   - Sub-menus: options for further navigation or transfer
   - Include option 9 or * to repeat
   - Include option 0 to transfer to operator

4. **Configure Business Hours**
   - Set the schedule and timezone
   - Set the after-hours and holiday menu IDs

5. **Set System Config**
   - Set the root menu ID
   - Configure default language and voice

6. **Add ANI/ALI Records** (optional)
   - Import known callers for VIP routing
   - Import location data for region-based routing

### Task: Add a New Department

1. Create a TTS prompt: "You've reached the [Department] department. For X, press 1..."
2. Create a menu node with the new prompt.
3. Add options pointing to transfer numbers or further sub-menus.
4. Go to the parent menu, add a new DTMF option pointing to the new menu.
5. If using speech routing, create a `TeamRoutingConfig` for the department.

### Task: Enable AI Speech Routing

1. Create team routing configs for each department (Team Routing is managed via Cosmos DB directly or future admin UI).
2. Create a speech routing prompt: "Please briefly describe why you're calling."
3. Create or edit a menu:
   - Set type to `SpeechRouting`
   - Enable speech recognition
   - Set the speech routing prompt
   - Add team routing config IDs
   - Set a fallback action (e.g., transfer to operator)
4. Test by calling and speaking naturally.

### Task: Connect an External System

1. Create an `ExternalSystemConfig` in Cosmos DB defining the API (base URL, endpoints, auth).
2. Create a `DataExtractionConfig` defining what data to extract from the caller's speech.
3. Add a menu option with action type `SubmitToExternalSystem` and the extraction config ID.
4. Test by calling and speaking a request that includes the required fields.

See [System Integration Guide](system-integration.md) for detailed configuration examples.

### Task: Block a Phone Number

1. Go to **ANI / ALI Data** > **ANI Records**.
2. Search for the phone number, or create a new ANI record.
3. Check the **Blocked** flag.
4. Save. Calls from this number will be immediately rejected.

### Task: Set Up a Holiday Schedule

1. Go to **Settings** > **Holidays**.
2. Click **Add Holiday**.
3. Enter the date and holiday name.
4. Ensure a Holiday Menu exists and is set in **Business Hours > Holiday Menu ID**.
5. Save. On that date, callers will hear the holiday menu instead of the regular menu.

### Task: Review a Specific Call

1. Go to **Call Logs**.
2. Use the date range and status filters to narrow results, or search by phone number.
3. Click a call row to open the detail drawer.
4. Review the complete menu path, transcript, intent, and any external system interactions.

### Task: Add an Existing PSTN Number (Direct Routing)

1. Ensure your SBC is registered with Azure Communication Services (see [PSTN Connectivity](system-integration.md#5-pstn-connectivity)).
2. Go to **Phone Numbers** > click **Add Phone Number**.
3. Enter the number in E.164 format (e.g., `+15551234567`).
4. Set **Number Type** to `DirectRouting`.
5. Enter the **SBC FQDN** (e.g., `sbc.contoso.com`) and **Port** (default: `5067`).
6. Select (or create) a **Root Menu** for this number — or leave blank to use the system-wide root menu.
7. Click **Save**.
8. Go to **Settings** > **General** and set **PSTN Mode** to `DirectRouting` or `Hybrid`.
9. Test by calling the number and verifying the correct IVR menu plays.

### Task: Configure Avaya CM10 VDN Transfer on Disconnect

1. Ensure your Avaya SBC is registered with Azure Communication Services.
2. In Avaya CM10, create VDNs and call vectors for post-IVR ACD routing.
3. Go to **Settings** > **Avaya CM10 Integration**.
4. Check **Transfer to VDN on Disconnect**.
5. Enter the **Default CM10 VDN** — either a SIP URI (`sip:70100@sbc.contoso.com`) or an E.164 number (`+18005550100`).
6. Enter the **CM10 SBC FQDN** if it differs from the main SBC.
7. Optionally set a **Transfer Prompt** to play before the handoff (e.g., "Please hold while we connect you.").
8. Click **Save Settings**.
9. Test by calling the IVR and hanging up. The call should transfer to the CM10 VDN instead of ending, and the call log should show disposition `TransferredToVdn`.

### Task: Add a VDN Transfer to a Menu Option

1. Go to **Call Flows** and open the target menu.
2. Add or edit a menu option (e.g., DTMF key `0` — "Speak to an Agent").
3. Set the action type to `TransferToVdn`.
4. Enter the **VDN Address** (e.g., `sip:70200@sbc.contoso.com` or extension `70200`). Leave blank to use the system default.
5. Optionally set a **Prompt ID** for a pre-transfer message.
6. Save the menu.

---

## Testing

Use the [PSTN & CM10 Simulator](pstn-simulator.md) to test call flows, DTMF navigation, VDN transfers, and agent routing from a browser without any telephony hardware.
