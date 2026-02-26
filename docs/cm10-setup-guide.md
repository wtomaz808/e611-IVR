# CM10 Setup Guide — Avaya Communication Manager with IVR

This guide covers how to configure an Avaya Communication Manager 10 (CM10) to work with the IVR system, including PSTN connectivity, VDN routing, vector programming, gateway/SBC configuration, and ACS Direct Routing.

## Architecture Overview

In this architecture, the **PSTN carrier delivers calls to CM10** (via SIP trunk or PRI). CM10 performs call processing (VDN/vector routing) and then **directs the call outbound through a gateway** (an SBC such as Avaya ASBCE, Session Manager, or a third-party SBC) to reach Azure Communication Services and the IVR.

```
            PSTN (Carrier)
                 │
           SIP Trunk (or PRI via Media Gateway)
                 │
    ┌────────────┴────────────┐
    │     Avaya CM10 Server   │
    │                         │
    │  Incoming Call Handling  │
    │         │               │
    │  ┌──────┴──────┐        │
    │  │ VDN → Vector│        │
    │  └──────┬──────┘        │
    │         │ route-to      │
    │  ┌──────┴────────────┐  │
    │  │ SIP Trunk (egress)│  │
    │  └──────┬────────────┘  │
    └─────────┼───────────────┘
              │ SIP
    ┌─────────┴───────────────┐
    │   Gateway / SBC         │  ← Avaya ASBCE, Session Manager,
    │   (Outbound Routing)    │    AudioCodes, Ribbon, Oracle
    └─────────┬───────────────┘
              │ SIP (Direct Routing)
    ┌─────────┴───────────────┐
    │  Azure Communication    │
    │  Services (ACS)         │
    └─────────┬───────────────┘
              │ API / Events
    ┌─────────┴───────────────┐
    │   IVR Functions         │
    │   (Azure Functions)     │
    └─────────────────────────┘
```

> **Key concept:** The "gateway" in this architecture is the **outbound egress device** that CM10 routes calls through to reach ACS. It is NOT an inbound PSTN termination device. In most modern deployments, this is an **Avaya Session Border Controller for Enterprise (ASBCE)** or **Avaya Session Manager**, though third-party SBCs (AudioCodes, Ribbon, Oracle) are also common.

### Alternate Topology: Legacy PRI with Media Gateway

If your carrier delivers PSTN via physical PRI T1/E1 circuits instead of SIP, you'll also have an **Avaya Media Gateway** (G430/G450/G860) that terminates those circuits and converts TDM to IP before handing the call to CM10. See [Appendix A: Legacy PRI Media Gateway](#appendix-a-legacy-pri-media-gateway) for that configuration.

## Prerequisites

| Component | Requirement |
|-----------|------------|
| Avaya CM10 | Version 10.x with SIP trunk support. Receives PSTN calls from carrier |
| Gateway / SBC | Avaya ASBCE, Session Manager, or third-party SBC (AudioCodes, Ribbon, Oracle). Routes outbound SIP from CM10 to ACS |
| Azure Communication Services | With Direct Routing enabled and phone numbers |
| Network | CM10 → Gateway/SBC on same LAN or routable network; SBC must have internet connectivity to Azure |
| Certificates | TLS certificate on the SBC (required by Azure Direct Routing) |
| PSTN Carrier | SIP trunk to CM10 (or PRI via media gateway — see Appendix A) |

## Step 1: CM10 Inbound PSTN Trunk

CM10 must receive PSTN calls from the carrier. In a SIP-based environment, the carrier delivers calls directly to CM10 via a SIP trunk.

### 1.1 Carrier SIP Trunk — Signaling Group

Configure the SIP signaling group for the inbound PSTN carrier:

```
add signaling-group N

  Group Number: N
  Group Type: sip
  Transport Method: tls
  Near-end Node Name: procr
  Near-end Listen Port: 5061
  Far-end Node Name: carrier-node
  Far-end Listen Port: 5060
  Far-end Network Region: 1
  Far-end Domain: sip.carrier.com
```

### 1.2 Carrier Trunk Group

```
add trunk-group N

  Group Number: N
  Group Type: sip
  Group Name: PSTN-Carrier
  TAC: N
  Direction: two-way
  Service Type: public-ntwrk
  Signaling Group: N  (from step 1.1)
  Number of Members: 100
```

> **Note:** If your carrier uses PRI instead of SIP, the inbound trunk terminates on a media gateway first. See [Appendix A](#appendix-a-legacy-pri-media-gateway).

## Step 2: CM10 Outbound Trunk to Gateway/SBC

CM10 also needs an **outbound SIP trunk** to the gateway/SBC so it can route calls to ACS for IVR processing.

### 2.1 Create a Signaling Group to the Gateway/SBC

```
add signaling-group N

  Group Number: N
  Group Type: sip
  Transport Method: tls
  Near-end Node Name: procr
  Near-end Listen Port: 5061
  Far-end Node Name: sbc-node
  Far-end Listen Port: 5061
  Far-end Network Region: 1
  Far-end Domain: sbc.yourdomain.com
```

### 2.2 Create a Trunk Group

```
add trunk-group N

  Group Number: N
  Group Type: sip
  Group Name: ACS-IVR-Trunk
  TAC: N
  Direction: two-way
  Service Type: public-ntwrk
  Signaling Group: N  (from step 2.1)
  Number of Members: 100
```

### 2.3 Define Route Pattern

```
add route-pattern N

  Pattern Number: N
  Grp No  FRL  NPA  Pfx  Hop  Runout  Runout Type
  N       0              1    next    unanswered
```

## Step 3: VDN Configuration (Incoming Call Routing)

VDNs (Vector Directory Numbers) are the entry points for calls. Each DID maps to a VDN.

### 3.1 Create VDNs

For each inbound call flow, create a VDN:

```
add vdn 70100
   Name: Main IVR
   Vector Number: 1
   
add vdn 70200
   Name: Sales Queue
   Vector Number: 2

add vdn 70300
   Name: Support Queue
   Vector Number: 3
```

### 3.2 Map DIDs to VDNs

Configure incoming call routing so each DID reaches the correct VDN. Calls arrive from the PSTN carrier trunk (Step 1) and are routed to VDNs via the incoming call handling table:

- **Incoming Call Handling Table:** In the carrier signaling group, map called numbers to VDNs

```
change inc-call-handling-trmt trunk-group N

Called Number    Dest Extension
+18005551000    70100
+18005551001    70200
+18005551002    70300
```

## Step 4: Vector Programming

Vectors control the call flow. For IVR integration, the vector routes the call **outbound through the gateway/SBC trunk** (Step 2) to ACS.

### 4.1 Main IVR Vector (Route to ACS via Gateway)

This vector sends the call out the gateway/SBC trunk to reach the IVR:

```
display vector 1

01 route-to number +18005551000 with cov n if unconditionally
```

**Explanation:** Step 1 routes the call out the trunk group (Step 2) to +18005551000. CM10 sends a SIP INVITE through the gateway/SBC, which forwards it to ACS via Direct Routing. ACS triggers the IVR's IncomingCall handler.

### 4.2 Queue Vector (for calls returning from IVR)

When the IVR transfers a call back to CM10 (e.g., to VDN 70200 for the sales queue):

```
display vector 2

01 queue-to skill 1 pri m
02 announcement 10001  "Thank you for holding. A sales representative will be with you shortly."
03 wait-time 30 secs hearing music
04 queue-to skill 1 pri m
05 announcement 10002  "We appreciate your patience. Please continue to hold."
06 wait-time 60 secs hearing music
07 goto step 4 if unconditionally
```

### 4.3 After-Hours Vector

```
display vector 3

01 time-of-day if in-queue 8:00 - 17:00 M-F goto step 3
02 route-to number afterhours_vdn with cov n if unconditionally
03 queue-to skill 2 pri m
04 wait-time 120 secs hearing music
05 disconnect after announcement 10003
```

## Step 5: Gateway / SBC Configuration

The **gateway** in this architecture is the **outbound routing device** that sits between CM10 and ACS. CM10 sends calls through this device when routing to the IVR. This is typically one of:

| Gateway Type | Description |
|-------------|-------------|
| **Avaya ASBCE** | Avaya Session Border Controller for Enterprise — Avaya's own SBC product |
| **Avaya Session Manager** | SIP routing/proxy that can route between CM10 and external SIP endpoints |
| **AudioCodes SBC** | Third-party SBC, commonly used with Azure Direct Routing |
| **Ribbon SBC** | Third-party SBC alternative |
| **Oracle SBC** | Third-party SBC (formerly Acme Packet) |

> **Important:** This is the device the user means when they say "the PSTN goes to CM10, and from there it is directed to the gateway." The gateway is the **egress point** for calls leaving CM10 to reach ACS/IVR.

The call flow through the gateway:

```
CM10 (SIP INVITE) → Gateway/SBC → Azure ACS (Direct Routing)
ACS (SIP response) → Gateway/SBC → CM10 (call established)
```

### 5.1 AudioCodes SBC Example

If using an AudioCodes SBC as the gateway:

**CM10 Side (IP Group):**

```
IP Group:
  Name: CM10
  Proxy Set: CM10_Proxy
  Media Realm: CM10_Realm
  SIP Transport: TLS
  Source URI: sbc.yourdomain.com
```

**ACS Side (IP Group):**

```
IP Group:
  Name: Azure-ACS
  Proxy Set: ACS_Proxy  (sip.pstnhub.microsoft.com)
  Media Realm: ACS_Realm
  SIP Transport: TLS
  Source URI: sbc.yourdomain.com
```

**Routing Rules:**

| Rule | Source | Destination | Description |
|------|--------|-------------|-------------|
| 1 | CM10 | Azure-ACS | CM10 → ACS (outbound to IVR) |
| 2 | Azure-ACS | CM10 | ACS → CM10 (IVR transfer back) |

### 5.2 Avaya ASBCE Example

If using the Avaya Session Border Controller for Enterprise:

1. **Enterprise Interface:** Configure a SIP Entity for CM10 (IP address, TLS port 5061)
2. **Public Interface:** Configure a SIP Entity for Azure ACS (sip.pstnhub.microsoft.com, TLS port 5061)
3. **Routing Policy:** Create rules to route between the CM10 and Azure ACS entities
4. **TLS Profile:** Install a public CA certificate (required by Azure Direct Routing)

### 5.3 TLS Certificate

Azure Direct Routing requires a trusted TLS certificate on the SBC:

1. Obtain a certificate from a public CA (DigiCert, GlobalSign, etc.)
2. Install the certificate on the SBC
3. Configure TLS profile to use the certificate
4. Common Name or SAN must match `sbc.yourdomain.com`

## Step 6: Azure ACS Direct Routing

### 6.1 Register the SBC

In Azure Portal → Communication Services → Direct Routing:

```
SBC FQDN: sbc.yourdomain.com
SBC Port: 5061
```

### 6.2 Create Voice Routing Rules

```json
{
  "name": "MainIVR",
  "numberPattern": "^\\+18005551\\d{3}$",
  "routes": [
    {
      "name": "CM10-Route",
      "sbcFqdn": "sbc.yourdomain.com"
    }
  ]
}
```

### 6.3 Verify Connectivity

```bash
# Test SBC health from Azure
az communication direct-routing sbc show \
  --sbc-fqdn sbc.yourdomain.com \
  --resource-name your-acs-resource

# Expected: Status = Online
```

## Step 7: IVR System Configuration

### 7.1 SystemConfig Settings

In the IVR Admin Portal → Settings → Avaya CM10 Integration:

| Setting | Value | Description |
|---------|-------|-------------|
| Enable Disconnect Transfer to VDN | ✅ | Transfer callers to CM10 on hangup |
| Default CM10 VDN | `70200` | VDN for disconnect transfers |
| CM10 SBC FQDN | `sbc.yourdomain.com` | SBC address for SIP URIs |
| CM10 SBC Port | `5061` | SBC SIP port |
| CM10 Transfer Prompt ID | *(optional)* | Prompt to play before transfer |

### 7.2 IVR Menu Configuration for VDN Transfers

Create menu actions that transfer callers to specific VDNs:

1. Open Admin Portal → Call Flows
2. Add a menu option with Action Type: **Transfer to VDN**
3. Set the VDN Address in one of these formats:

| Format | Example | Description |
|--------|---------|-------------|
| SIP URI | `sip:70200@sbc.yourdomain.com` | Full SIP address (preferred) |
| Extension | `70200` | Extension number (uses system SBC config) |
| E.164 | `+170200` | Phone number format |

### 7.3 Environment Variables

```env
# IVR Functions .env
ACS_CONNECTION_STRING=endpoint=https://your-acs.communication.azure.com/;accesskey=...
CALLBACK_BASE_URL=https://your-ivr.azurewebsites.net
```

## Step 8: Testing

### 8.1 Using the PSTN Simulator (Mock Mode)

The fastest way to test the full call flow without any hardware:

```bash
# Start the simulator
cd simulator
docker compose up -d

# Configure IVR to use mock ACS
# ACS_CONNECTION_STRING=endpoint=http://pstn-simulator:8080;accesskey=bW9ja2tleQ==

# Open simulator: http://localhost:5200
# 1. Select a caller and DID
# 2. Click "Call"
# 3. Interact with DTMF/speech as prompted
# 4. Watch the call flow through CM10 VDN → IVR → Agent
```

### 8.2 Using the PSTN Simulator (Live Mode)

Test with real ACS and a real SBC:

```bash
# Set Live mode in simulator .env
ACS_MODE=Live
ACS_CONNECTION_STRING=endpoint=https://your-acs.communication.azure.com/;accesskey=...
ACS_CALLER_NUMBER=+15551234567

# Start the simulator
docker compose up -d

# Open simulator and place a call
# The call uses real ACS → Direct Routing → SBC → CM10
```

### 8.3 End-to-End Verification Checklist

| Step | Test | Expected Result |
|------|------|-----------------|
| 1 | Call DID +18005551000 | PSTN carrier delivers call to CM10 |
| 2 | CM10 receives call | Incoming call handling routes to VDN 70100 |
| 3 | VDN executes vector | Vector routes call out SIP trunk to gateway/SBC |
| 4 | Gateway/SBC forwards to ACS | ACS receives SIP INVITE via Direct Routing |
| 5 | ACS triggers EventGrid | IVR receives IncomingCall event |
| 6 | IVR answers | Welcome prompt plays |
| 7 | Caller presses DTMF | IVR recognizes and navigates menu |
| 8 | IVR transfers to VDN | SIP REFER/INVITE to sip:70200@sbc |
| 9 | Gateway/SBC routes back to CM10 | CM10 receives call on VDN 70200 |
| 10 | VDN 70200 executes queue vector | Agent placed in ACD queue |
| 11 | Agent assigned | Agent station rings, caller connected |
| 12 | Caller disconnects | CDR logged |

## Common CM10 Administration Commands

```
# ─── Trunks ──────────────────────────────────────
# Display carrier trunk group status (inbound PSTN)
status trunk-group N

# Display gateway/SBC trunk group status (outbound to ACS)
status trunk-group N

# Display signaling group status (SIP trunk to gateway/SBC)
status signaling-group N

# ─── VDNs & Vectors ─────────────────────────────
# Display VDN status
status vdn 70100

# Display vector details
display vector 1

# ─── Calls & Agents ─────────────────────────────
# Monitor active calls
list measurements call-summary last-hour

# Display agent login status
list agent-loginid

# Trace an active call
status station EXTENSION

# ─── Media Gateway (if using PRI — see Appendix A) ──
# Display media gateway registration and health
status media-gateway N

# Display DS1 span status
status ds1 N

# List all registered gateways
list media-gateway
```

## Call Flow: IVR Disconnect Transfer to VDN

When a caller hangs up during the IVR, the system can automatically transfer them to a CM10 VDN:

```
1. Caller disconnects during IVR interaction
2. IVR detects CallDisconnected event
3. IVR checks SystemConfig.EnableDisconnectTransferToVdn = true
4. IVR resolves VDN address:
   - If SystemConfig.Cm10SbcFqdn is set: sip:70200@sbc.yourdomain.com
   - Else uses DefaultCm10Vdn as E.164
5. IVR calls TransferCallToParticipantAsync with the VDN address
6. ACS sends SIP INVITE to SBC
7. SBC routes to CM10
8. CM10 receives call on VDN 70200
9. Vector 2 executes: queue-to skill → agent assigned
```

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Calls not reaching CM10 | Carrier SIP trunk status — `status signaling-group N`; carrier firewall/routing |
| VDN not routing | Check VDN → vector assignment, vector logic |
| Calls not reaching gateway/SBC | Verify outbound trunk group + signaling group status — `status trunk-group N` |
| Gateway/SBC not forwarding to ACS | Verify SBC routing rules, TLS cert, IP groups, SBC logs |
| ACS not receiving calls | Check Direct Routing SBC registration status in Azure Portal |
| IVR not answering | Check EventGrid subscription, IVR logs |
| Transfer to VDN fails | Verify SBC FQDN, VDN number, SIP URI format |
| Agent not ringing | Check ACD queue, agent login status, skill assignments |
| "503 Service Unavailable" | Trunk capacity exceeded or gateway/SBC connectivity issue |
| One-way audio | Check codec negotiation, NAT traversal on SBC, network region settings |
| PRI span in alarm (red/yellow) | *(Legacy PRI only)* `status ds1 N` — physical cable, carrier provisioning |
| Gateway not registered | *(Legacy PRI only)* `status media-gateway N` — IP, network, firewall |

---

## Appendix A: Legacy PRI Media Gateway

If your PSTN carrier delivers calls via **physical PRI T1/E1 circuits** instead of SIP, you need an **Avaya Media Gateway** to terminate those circuits and convert TDM to IP before passing calls to CM10.

In this topology, the call flow is:

```
PSTN (PRI T1/E1) → Media Gateway (TDM→IP) → CM10 → Gateway/SBC → ACS → IVR
```

### Gateway Models

| Model | Use Case | Max Ports | Interface |
|-------|----------|-----------|----------|
| **G430** | Small branch office | 2 T1/E1 (46 channels) + 24 analog | PRI, analog, SIP |
| **G450** | Mid-size office | 10 T1/E1 (230 channels) + 80 analog | PRI, BRI, analog, SIP |
| **G860** | Large/data center | Up to 450 DS1 + large analog | PRI, SIP, high-density |
| **VGMC** | Virtualized/cloud | Software-defined capacity | SIP only (no TDM) |

### A.1 Physical Gateway Installation

1. **Rack and cable** the gateway in the telecom closet
2. **Connect PRI T1/E1 spans** from the PSTN carrier to the gateway's DS1 media modules
3. **Connect the gateway to the LAN** — the gateway and CM10 server must be on a routable IP network
4. **Assign the gateway an IP address** via the front-panel LCD or serial console

### A.2 Register the Gateway with CM10

```
add media-gateway N

  Number: N
  Type: g450           (or g430, g860)
  Name: GW-Main
  Serial No: (from gateway label)
  Link Encryption Type: any-media
  Network Region: 1
  Recovery Rule: 1
  IP Address: 10.10.1.50   (gateway's LAN IP)
```

Verify registration:

```
status media-gateway N

  Gateway Name: GW-Main
  IP Address: 10.10.1.50
  Registration State: registered
  FW Version: 40.22.0
```

### A.3 Configure DS1 Spans

Each physical PRI circuit on the gateway must be defined as a DS1:

```
add ds1 N

  DS1 Number: N
  Name: PSTN-PRI-1
  Interface Type: t1        (or e1 for European)
  Signaling Type: isdn-pri
  Bit Rate: 1.544 Mbps
  Line Coding: b8zs
  Framing: esf
  Gateway: N               (media gateway from step A.2)
  Port: 01A01              (slot/port on the gateway)
```

### A.4 Assign DS1 to a Trunk Group

```
add trunk-group N

  Group Number: N
  Group Type: isdn
  Group Name: PSTN-PRI
  TAC: N
  Direction: two-way
  Service Type: public-ntwrk
  Signaling Group: N
  Number of Members: 23    (23 B-channels per T1)

  # Assign DS1 members:
  Port    Siggrp
  01A0101 N
  01A0102 N
  ... (up to 01A0123)
```

### A.5 Gateway Survivability (Optional)

The gateway can act as a **Local Survivable Processor (LSP)** if the CM10 server goes down:

```
change media-gateway N

  Survivable GK Node Name: gw-lsp
  Survivable COR: 1
  Survivable Trunk Dest: yes
```
