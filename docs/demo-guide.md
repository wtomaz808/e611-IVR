# E911 IVR Demo Guide

**Audience:** Customer stakeholders, sales engineers, program managers  
**Purpose:** Step-by-step walkthrough for demonstrating the Admin Portal and PSTN Simulator  
**Environment:** `https://ivr-teams-simulator-hgknk444g237w.azurewebsites.us` (Simulator) / `https://ivr-teams-admin-hgknk444g237w.azurewebsites.us` (Admin Portal)

---

## Overview

The E911 IVR system has two interfaces you will show:

| Interface | URL | Role in Demo |
|---|---|---|
| **Admin Portal** | `…-admin-….azurewebsites.us` | Configuration — menus, prompts, caller data, call logs |
| **PSTN Simulator** | `…-simulator-….azurewebsites.us` | "The Phone" — drive live calls from a browser |

Open both in separate browser tabs. You will narrate what the simulator is doing while the audience watches the Admin Portal respond in real time.

---

## Part 1 — Admin Portal

### 1.1 Login and Dashboard

1. Navigate to the Admin Portal URL.
2. Sign in with your Microsoft Entra ID account.
3. The **Dashboard** loads automatically.

**What to show:**

| Element | Talking Point |
|---|---|
| **Today's Calls** card | Live counter — increments as simulator calls arrive |
| **Status** card | "Open" or "After Hours" — driven by the Business Hours schedule you configure |
| **Call Disposition chart** | 7-day breakdown: Completed, Transferred, Abandoned |
| **Calls by Hour** histogram | Peak-hour patterns — useful for staffing decisions |
| **Recent Calls table** | Shows caller number, ANI name, status, and disposition in real time |

> **Demo tip:** Keep this page visible on the audience's screen. As calls come in from the simulator, they will watch the counters and table update without any manual refresh.

---

### 1.2 Call Flow Builder

**Route:** Sidebar → **Call Flows**

This is how the IVR menu tree is configured. No code changes, no redeploy.

#### Show the Existing Menu Tree

1. The left panel lists all menus hierarchically. Expand **Main Menu**.
2. Click **Main Menu** to open the detail panel.
3. Point out:
   - **Prompt**: the TTS or audio file that plays when callers enter this menu
   - **Timeout** and **Max Retries**: how long the system waits before replaying
   - **Options** list: each DTMF key (1, 2, 3…) and its action

#### Talking Points for the Menu Builder

| Feature | What It Demonstrates |
|---|---|
| Hierarchical tree | Multi-level IVR — unlimited depth without code changes |
| Action Types | Navigate → submenu, Transfer → VDN/number, Play Prompt, Webhook, External System |
| Menu Conditions | Route by time of day, caller type, VIP status, geographic region |
| Root Menu flag | One number in, multiple menu trees depending on the DID dialed |

#### Live Edit (optional)

1. Select **Main Menu** → click **Edit**.
2. Change the **Timeout** from `10` to `5` seconds.
3. Click **Save**.
4. Place a new call from the Simulator and let it time out — it will reprompt faster.
5. Reset to `10` when done.

> **Key message:** Changes take effect on the **next call** — no redeploy required.

---

### 1.3 Prompt Management

**Route:** Sidebar → **Prompts**

#### Show Prompt Types

| Type | How It Works |
|---|---|
| **TTS** | Text stored in Cosmos DB → synthesized to audio by Azure Cognitive Services at call time; cached in Blob Storage |
| **Audio File** | Pre-recorded WAV/MP3 uploaded to Blob Storage |
| **SSML** | Advanced markup for dynamic content (e.g., inserting caller name into the greeting) |

#### What to Click

1. Find `prompt-welcome` in the list — this is the first thing callers hear.
2. Click **Edit** and show the TTS text field: `"Thank you for calling E911 Emergency Services. Press 1 for Emergency Dispatch..."`
3. Point out the **Voice** and **Style** selectors (neural voices from Azure Cognitive Services).
4. Click **Cancel** (no changes needed).

> **Key message:** The whole audio experience can be changed without uploading files — just type new text and save.

---

### 1.4 ANI / ALI Data

**Route:** Sidebar → **ANI / ALI Data**

#### ANI Records Tab

ANI = Automatic Number Identification — maps a caller's phone number to their identity and routing preferences.

| Column | What It Enables |
|---|---|
| **Caller Name** | Shows in call logs and dashboard instead of raw number |
| **VIP Flag** | Bypasses the normal menu tree and goes straight to a custom menu |
| **Blocked** | Immediately rejects the call — no prompts, no log entry |
| **Priority** | Higher-priority callers get preference in queue if implemented |
| **Custom Routing Menu** | Personalized IVR experience for specific callers or organizations |

#### ALI Records Tab

ALI = Automatic Location Identification — associates a phone number with a physical address.

| Field | E911 Use Case |
|---|---|
| Street / City / State | Displayed in call logs for dispatcher situational awareness |
| Service Area | Used in conditional routing to route to the correct regional PSEC |
| Region | Menu condition: "if caller is in Region A, go to Menu A" |
| Timezone | Correct business-hours evaluation for callers across time zones |

#### Demo Scenario

1. Click on any VIP caller record.
2. Show the `CustomRoutingMenuId` field — point out this bypasses the main menu.
3. Return to the Simulator and place a call using that VIP number.
4. Watch the Admin Portal → Call Logs → the call shows the custom menu path.

---

### 1.5 Call Logs

**Route:** Sidebar → **Call Logs**

#### Table View

| Column | What to Highlight |
|---|---|
| **Caller** | E.164 number |
| **Caller Name** | Resolved from ANI data — proves caller ID is working |
| **Status** | Color-coded: Completed (green), Transferred (blue), Failed (red) |
| **Duration** | Full call duration |
| **Disposition** | TransferredToExternal, CallerHangup, SystemError, etc. |

#### Call Detail Drawer

Click any row to open the detail drawer:

1. **Call Information** — call ID, start/end time, duration, status
2. **ANI Data** — name, account, VIP status, language
3. **ALI Data** — full address and region
4. **Menu Path** — step-by-step trace of every menu visited:
   ```
   Main Menu → [pressed 1] → Emergency Dispatch → [transferred]
   ```
5. **Transcript** (speech calls) — exact words spoken, AI-detected intent, confidence score

> **Key message:** Full audit trail for every call. Searchable and filterable. 90-day TTL keeps storage costs manageable.

---

### 1.6 Settings

**Route:** Sidebar → **Settings**

#### General Tab — Key Settings to Show

| Setting | Talking Point |
|---|---|
| **Root Menu ID** | The entry point for all calls — change this to A/B test different menu trees |
| **Enable Speech Recognition** | Toggle AI-powered natural language routing system-wide |
| **Max Call Duration** | Safety valve — prevents runaway calls |
| **Default TTS Voice** | One change updates the voice in all TTS prompts |

#### Business Hours Tab

Show the 7-day schedule grid:

- Toggle a day off → all calls on that day route to the **After-Hours Menu** automatically
- Set the **Timezone** correctly (critical for government deployments spanning multiple zones)
- **Holiday list** — add dates that should trigger the holiday menu

#### VDN Transfer Settings (Avaya CM10 Integration Tab)

For customers with existing Avaya CM10:

| Setting | What It Does |
|---|---|
| **Transfer to VDN on Disconnect** | When a caller hangs up, the IVR transfers the call context to the CM10 VDN for ACD disposition |
| **Default CM10 VDN** | The SIP URI or E.164 address of the Avaya CM10 VDN |

> **Key message:** No rip-and-replace. The IVR integrates with existing Avaya infrastructure — calls flow through Teams, navigate the IVR, then hand off to CM10 for ACD routing.

---

## Part 2 — PSTN Simulator

**URL:** `https://ivr-teams-simulator-hgknk444g237w.azurewebsites.us`

The simulator acts as a fully functional "phone" that drives calls into the IVR using the same Microsoft Graph Calling API protocol that real Teams Phone System uses. No real PSTN, no real phone numbers — everything runs in the browser.

---

### 2.1 Simulator Overview

**What the audience sees:**
- Left panel: call controls and event trace
- Right panel (optional): CM10 administration view

**Key UI elements:**

| Element | Purpose |
|---|---|
| **Caller dropdown** | Select from pre-seeded ANI records (Regular, VIP, Government) |
| **DID dropdown** | Choose which phone number to dial (each maps to a different IVR menu) |
| **Place Call button** | Initiates the call — simulator sends `commsNotification (incoming)` to IVR |
| **DTMF Keypad** | Active once IVR enters `IvrRecognizing` state — sends tone to IVR |
| **Speech Input box** | Type spoken words for AI-routing scenarios |
| **Hang Up button** | Terminates the call from the caller side |
| **Reset button** | Clears call state if something goes wrong |
| **Event Trace** | Color-coded timeline of every message exchanged (IVR ↔ Simulator) |

---

### 2.2 Demo Scenario 1 — Basic DTMF Emergency Call

**Goal:** Show a caller dialing the E911 line, navigating DTMF menus, and the call log appearing in real time.

**Steps:**

1. In the **Simulator**, select caller: `JBPHH — HQ Pacific Fleet` (a military facility from the ANI database)
2. Select DID: `+17035550911` (E911 Main Line)
3. Click **Place Call**
4. Watch the **Event Trace** populate:
   - `commsNotification (incoming)` — Simulator → IVR
   - `Graph PATCH /calls/{id}` — IVR answers
   - `commsNotification (established)` — Simulator confirms
   - `Graph POST playPrompt` — IVR plays welcome audio
   - `commsNotification (playPromptOperation complete)` — Simulator confirms
   - `Graph POST subscribeToTone` — IVR listens for DTMF
5. In the Simulator, press **1** (Emergency Dispatch)
6. Watch:
   - `commsNotification (toneReceived: tone1)` — Simulator → IVR
   - `Graph POST /transfer` — IVR transfers call
   - Call status transitions to **Transferred**
7. Switch to **Admin Portal → Call Logs**
   - Find the call — it shows:
     - Caller: `+18085551234` → Name: `JBPHH — HQ Pacific Fleet`
     - Menu Path: `Main Menu → [1] → Emergency Dispatch → [transferred]`
     - Disposition: `TransferredToExternal`

**Talking points:**
- The caller was identified by ANI before the phone was even answered
- The full chain of custody is logged — every key press, every menu, every transfer

---

### 2.3 Demo Scenario 2 — VIP Caller Bypass

**Goal:** Show how VIP callers skip the main menu and get a personalized experience.

**Steps:**

1. In the Simulator, select a VIP caller (e.g., `RDC — Remote Dispatch` — marked VIP in ANI database)
2. Select DID: `+17035550911`
3. Click **Place Call**
4. Note: the IVR plays a different welcome prompt and goes directly to a specialized menu
5. In **Admin Portal → Call Logs**, click the call — notice the **ANI Data** section shows `VIP: true`

**Talking point:** The ANI database is pre-populated with your organization's phone directory. High-priority callers or known facilities get a streamlined experience automatically.

---

### 2.4 Demo Scenario 3 — After-Hours Routing

**Goal:** Show automatic time-of-day routing without any menu changes.

**Steps:**

1. In **Admin Portal → Settings → Business Hours**, temporarily set today to "Closed"
2. Place a call from the Simulator
3. The IVR plays the **After-Hours** prompt instead of the main menu
4. Reset the schedule when done

**Talking point:** No code change, no redeploy. The duty officer can flip the schedule from the portal and every subsequent call routes differently.

---

### 2.5 Demo Scenario 4 — Menu Path Audit

**Goal:** Show that every action is traceable.

**Steps:**

1. Place a call, navigate through several menus (e.g., press 2 → Alarm Admin, press 1 → Alarm Status)
2. Hang up
3. In **Admin Portal → Call Logs**, open the call detail drawer
4. Scroll to **Menu Path**:
   ```
   Main Menu → [pressed 2] → Alarm Admin → [pressed 1] → Alarm Status → [caller hung up]
   ```

**Talking point:** Every single key press, every branch taken, timestamped. This is the audit trail your compliance and safety teams need.

---

### 2.6 Simulator Event Trace — What to Highlight

The event trace panel shows the raw message exchange between the simulator and the IVR. Point this out to technical audiences:

| Color | Source | Meaning |
|---|---|---|
| Blue | PSTN (Simulator) | Notifications sent to IVR |
| Green | IVR (Functions) | Graph API calls made by IVR |
| Orange | Graph API (Mock) | Simulator's response to IVR's Graph calls |
| Gray | System | State transitions and internal events |

> **For technical audiences:** "This is the exact same protocol as real Microsoft Teams Phone System. The simulator sends `commsNotifications` and responds to Graph API calls identically to how Teams would. When we connect to a real Teams tenant, zero IVR code changes are needed — we just point `GraphApiEndpoint` at `graph.microsoft.us`."

---

## Part 3 — Connecting to a Real Teams Tenant

When the customer has a Microsoft Teams tenant ready:

1. Register the IVR as a **Teams Resource Account** in the customer's tenant
2. Assign the resource account a **Phone System license** and a **Direct Routing number**
3. Update the IVR Function App settings:
   - `MicrosoftAppId` → Bot App Registration Client ID
   - `MicrosoftAppPassword` → Bot secret
   - `GraphApiEndpoint` → `https://graph.microsoft.us/v1.0` (Gov) or `https://graph.microsoft.com/v1.0`
   - `ChannelService` → `https://botframework.azure.us` (Gov)
4. Update the Bot Framework registration to point to the Function App URL

The simulator remains available for regression testing and scenario validation alongside the live system.

---

## Demo Checklist

Before presenting, verify:

- [ ] Admin Portal loads and shows dashboard with data
- [ ] Simulator loads and "Place Call" button is visible
- [ ] At least one call completes end-to-end (welcome audio plays, keypad becomes active)
- [ ] Call log appears in Admin Portal after call
- [ ] ANI records show caller names (not raw numbers) in call logs
- [ ] Business hours setting is set to "Open" for the demo time
- [ ] Hang Up button clears the call UI

---

## Troubleshooting During Demo

| Symptom | Quick Fix |
|---|---|
| Call stuck at "Incoming", never "Established" | IVR Functions may have restarted — wait 30 sec and retry |
| No audio / "We're sorry" message | TTS cache warming — the second call will have audio |
| DTMF keypad grayed out | Wait for `IvrRecognizing` state in event trace (a few seconds after established) |
| Call log not appearing | Refresh the Admin Portal Call Logs page |
| Hang Up button doesn't clear | Click **Reset** button to force-clear call state |
