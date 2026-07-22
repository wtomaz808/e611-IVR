# Avaya to Azure Communication Services Integration
## Talking Points for Avaya SBC Direct Routing Meeting

**Meeting Date:** June 30, 2026  
**Purpose:** Configure Avaya Communication Manager, SIP Trunk, and SBC for Azure Communication Services Direct Routing integration  
**Environment:** Azure Government Cloud

---

## Executive Summary

We're implementing an E911 IVR system on Azure that requires integration with our existing Avaya Communication Manager infrastructure. We need to route incoming PSTN calls through Avaya CM10 → SBC → Azure Communication Services (ACS) using Direct Routing, triggering our cloud-based IVR Functions.

---

## 1. Use Case Overview

### Business Objective
Deploy a **cloud-native Emergency 911 IVR system** that:
- Receives inbound PSTN calls through existing Avaya infrastructure
- Performs ANI/ALI lookups and multi-level menu navigation
- Uses Azure OpenAI for intelligent call routing
- Provides real-time call management through an admin portal
- Integrates with external systems via webhooks

### Why Keep Avaya in the Flow?
- **Leverage existing PSTN connectivity** - Our carrier terminates to Avaya CM10
- **Maintain centralized call control** - Avaya handles initial call processing and routing decisions
- **Compliance requirements** - Need enterprise-grade call recording and audit trails
- **Hybrid flexibility** - Some calls may route to traditional Avaya endpoints, others to Azure IVR

---

## 2. Target Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CALL FLOW                                   │
└─────────────────────────────────────────────────────────────────────┘

    PSTN Carrier (SIP Trunk)
              ↓
    ┌─────────────────────┐
    │  Avaya CM10         │ ← Receives inbound calls
    │  (Call Manager)     │ ← VDN/Vector routing logic
    └──────────┬──────────┘
               ↓ (Outbound SIP Trunk)
    ┌─────────────────────┐
    │  Session Border     │ ← NAT/Firewall traversal
    │  Controller (SBC)   │ ← TLS termination
    │                     │ ← SIP normalization
    └──────────┬──────────┘
               ↓ (Direct Routing - TLS 1.2+)
    ┌─────────────────────┐
    │  Azure Communication│ ← ACS FQDN: *.pstnhub.microsoft.com
    │  Services (ACS)     │ ← Custom Domain: acs.yourdomain.com
    └──────────┬──────────┘
               ↓ (Event Grid)
    ┌─────────────────────┐
    │  Azure Functions    │ ← IVR Engine
    │  (IVR Logic)        │ ← Call flow automation
    └─────────────────────┘
```

---

## 3. Key Integration Points

### 3.1 Avaya Communication Manager (CM10) Configuration

**What We Need from Avaya:**

#### A. Outbound SIP Trunk to SBC
```
Purpose: Route IVR-bound calls from CM10 to the SBC

Configuration Requirements:
- Trunk Type: SIP
- Direction: Two-way (primarily outbound for IVR)
- Transport: TLS preferred (secure), TCP acceptable
- Signaling Group: Points to SBC FQDN/IP
- Trunk Access Code (TAC): Unique identifier
- Route Pattern: Defines call routing logic
```

**Example Configuration:**
```
add signaling-group <N>
  Group Type: sip
  Transport Method: tls
  Far-end Node Name: sbc.yourdomain.com
  Far-end Listen Port: 5061
  Far-end Domain: sbc.yourdomain.com

add trunk-group <N>
  Group Type: sip
  Group Name: ACS-IVR-Trunk
  Service Type: public-ntwrk
  Signaling Group: <N>
```

#### B. VDN and Vector Programming
```
Purpose: Define which calls route to Azure IVR

Configuration Needed:
- VDN (Vector Directory Number) for each DID
- Vector programming logic:
  * Collect digits (if needed)
  * Route-to trunk (the ACS-IVR-Trunk)
  * Fallback handling (if Azure unavailable)
```

**Example Vector:**
```
Vector 100 (IVR-E911)
01  wait-time 2 secs hearing ringback
02  route-to number +14445556666 with coverage n trunk-group <N>
03  stop
```

**Key Question for Avaya:** 
- What DIDs should route to the IVR?
- Do we need any pre-processing in Avaya before handoff to Azure?
- What's the fallback strategy if Azure IVR is unreachable?

---

### 3.2 Session Border Controller (SBC) Configuration

**This is the CRITICAL component** - the SBC bridges Avaya's private network to Azure's public cloud.

#### SBC Requirements - MUST HAVE

| Requirement | Details | Why It Matters |
|-------------|---------|----------------|
| **Certified for ACS Direct Routing** | AudioCodes, Ribbon SBC, Oracle, Avaya SBCE | Azure only supports certified SBCs |
| **TLS 1.2+ Support** | Valid public CA-signed certificate | Azure rejects self-signed or expired certs |
| **FQDN (Not IP)** | sbc.yourdomain.com (public DNS) | Azure Direct Routing requires FQDN |
| **SIP Trunk Profile** | Microsoft 365 / Azure preset | Proper SIP header handling |
| **Media Transcoding** | G.711 µ-law / a-law | Azure ACS media format requirement |
| **Public IP Address** | Static public IP with port forwarding | Azure initiates connections to SBC |

#### Certified SBC Options

**Option 1: Avaya Session Border Controller for Enterprise (SBCE)**
- ✅ Native Avaya integration
- ✅ Microsoft-certified for Teams/ACS Direct Routing
- ✅ Pre-configured templates for Azure
- ⚠️  Requires license and firmware updates

**Option 2: AudioCodes Mediant SBC**
- ✅ Widely deployed for Azure integrations
- ✅ Excellent documentation and support
- ✅ Web-based configuration wizard for ACS

**Option 3: Ribbon SBC (Sonus/GENBAND)**
- ✅ Enterprise-grade reliability
- ✅ Strong Avaya interoperability
- ✅ Microsoft-certified

**Question for Avaya:** 
- Which SBC model do you currently have?
- Is it on the Microsoft certified SBC list for Direct Routing?
- What firmware version is running?

#### SBC Configuration - Inbound (from Avaya CM)

```
Purpose: Receive calls from Avaya CM10 trunk

Configuration:
- SIP Trunk Profile: "Avaya CM"
- Listening Port: 5060 (TCP) or 5061 (TLS)
- Allowed Source IP: <Avaya CM10 IP>
- Realm/Context: Internal
- Media: Allow G.711 µ-law/a-law
```

#### SBC Configuration - Outbound (to Azure ACS)

```
Purpose: Forward calls to Azure Communication Services

Configuration:
- SIP Trunk Profile: "Microsoft 365" or "Azure ACS"
- Destination: sip.pstnhub.microsoft.com (Azure Government)
              OR region-specific FQDN
- Transport: TLS 1.2 (port 5061)
- Certificate: Valid public CA cert matching SBC FQDN
- SIP Headers:
  * P-Asserted-Identity: +1NXXNXXXXXX (caller ID)
  * Contact: sbc.yourdomain.com
  * From/To: Normalized E.164 format
- Media Encryption: SRTP (required by Azure)
```

#### Critical SBC Settings

**SIP Normalization Rules:**
```
Purpose: Ensure number formats match what Azure expects

Inbound (from CM):
- Strip internal extensions
- Prefix country code (+1 for US)
- Format: +1NXXNXXXXXX (E.164)

Outbound (to Azure):
- Use custom ACS domain (e.g., acs.yourdomain.com)
- Include P-Asserted-Identity header
```

**Media Handling:**
```
Codec Priority:
1. G.711 µ-law (PCMU)
2. G.711 a-law (PCMA)

Media Security:
- SRTP encryption enabled
- DTLS handshake support
- Disable comfort noise (VAD off)
```

**Firewall/NAT Rules:**
```
Required Ports:
- SIP Signaling: 5061 (TLS outbound to Azure)
- Media (RTP): 49152-53247 (UDP bidirectional)

Azure IP Ranges:
- Download Azure Gov IP ranges (JSON)
- Whitelist "AzureCommunicationServices" service tag
```

---

### 3.3 Azure Communication Services (ACS) Configuration

**What We Configure on Azure Side (for your awareness):**

#### A. Custom Domain Setup
```
Purpose: Establish trust between SBC and Azure

Steps:
1. Register domain (e.g., acs.yourdomain.com) in ACS
2. Add DNS TXT record for verification
3. Wait for Azure to validate ownership (~5-15 mins)
4. Domain status: Verified ✅
```

**Example DNS Record:**
```
Type:  TXT
Host:  acs.yourdomain.com
Value: microsoft-domain-verification=abc123...
TTL:   3600
```

#### B. Add SBC to Direct Routing
```
Purpose: Whitelist your SBC as trusted endpoint

Configuration in Azure Portal:
- SBC FQDN: sbc.yourdomain.com
- SIP Port: 5061
- Max Concurrent Sessions: 100 (adjust as needed)
- Enabled: True
```

**PowerShell Example:**
```powershell
# Add SBC to ACS Direct Routing
New-CsOnlinePSTNGateway `
    -Identity "sbc.yourdomain.com" `
    -SipSignalingPort 5061 `
    -MaxConcurrentSessions 100 `
    -Enabled $true
```

#### C. Phone Number Routing (Voice Routing Policy)
```
Purpose: Map incoming numbers to IVR call flow

Configuration:
- Phone Number: +14445556666 (example)
- Routing: Direct Routing via sbc.yourdomain.com
- Event Grid Webhook: Triggers Azure Function IVR
```

---

## 4. Network and Security Requirements

### 4.1 Network Connectivity

**Avaya CM to SBC:**
- Private network connectivity (same VLAN or routed)
- Low latency (<50ms recommended)
- QoS marking (DSCP EF for voice traffic)

**SBC to Azure:**
- Public internet egress with static IP
- Bandwidth: ~100 Kbps per concurrent call
- Azure Government IP ranges whitelisted

### 4.2 Security Posture

**TLS Certificate Requirements:**
```
Certificate Attributes:
- Issued by: Public CA (DigiCert, GlobalSign, Let's Encrypt)
- Subject CN: sbc.yourdomain.com
- SAN: sbc.yourdomain.com
- Key Size: 2048-bit RSA minimum
- Expiration: Must be current (Azure checks validity)
- Chain: Complete chain of trust to root CA
```

**SBC Security Hardening:**
- Disable non-TLS transports for Azure trunk
- Enable SIP authentication (Azure validates certificate)
- Implement toll fraud protection rules
- Rate limiting on inbound/outbound calls

### 4.3 Quality of Service (QoS)

**Recommended Settings:**
```
SIP Signaling:
- DSCP: CS3 (24)
- Priority: Medium

RTP Media:
- DSCP: EF (46) 
- Priority: High
- Jitter Buffer: 60-120ms
```

---

## 5. Call Flow Detail (Step-by-Step)

### Typical Inbound Call Journey

```
1. PSTN Carrier → Avaya CM10 (SIP trunk)
   - Caller dials +1-444-555-6666
   - Carrier routes to CM10 SIP trunk
   
2. Avaya CM10 VDN/Vector Processing
   - Incoming call hits VDN 6666
   - Vector 100 executes:
     * Plays announcement (optional)
     * route-to trunk-group ACS-IVR-Trunk
   
3. CM10 → SBC (Outbound SIP Trunk)
   - SIP INVITE to sbc.yourdomain.com
   - Headers: From: +1NXXNXXXXXX, To: +14445556666
   
4. SBC Normalization
   - Validates number format (E.164)
   - Adds P-Asserted-Identity header
   - Establishes TLS connection to Azure
   
5. SBC → Azure ACS (Direct Routing)
   - SIP INVITE to sip.pstnhub.microsoft.com
   - TLS 1.2 encrypted signaling
   - SRTP encrypted media
   
6. Azure ACS → Event Grid
   - Publishes "IncomingCall" event
   - Includes ANI, DNIS, call ID metadata
   
7. Event Grid → Azure Function (IVR)
   - Triggers IncomingCallHandler function
   - IVR answers call, plays prompts
   - Captures DTMF or speech input
   
8. Azure Function → ACS Call Automation API
   - Controls call flow (play, recognize, transfer)
   - Executes business logic
   
9. Call Completion
   - IVR terminates call or transfers to agent
   - Azure logs call details to Cosmos DB
```

### SIP Message Example

**INVITE from SBC to Azure:**
```
INVITE sip:+14445556666@sip.pstnhub.microsoft.com SIP/2.0
Via: SIP/2.0/TLS sbc.yourdomain.com:5061;branch=z9hG4bK1234
From: <sip:+12223334444@sbc.yourdomain.com>;tag=abc123
To: <sip:+14445556666@acs.yourdomain.com>
P-Asserted-Identity: <sip:+12223334444@sbc.yourdomain.com>
Contact: <sip:+12223334444@sbc.yourdomain.com:5061;transport=tls>
Call-ID: unique-call-id@sbc.yourdomain.com
CSeq: 1 INVITE
Content-Type: application/sdp

v=0
o=- 123456 123456 IN IP4 203.0.113.10
s=-
c=IN IP4 203.0.113.10
t=0 0
m=audio 50000 RTP/SAVP 0 8
a=rtpmap:0 PCMU/8000
a=rtpmap:8 PCMA/8000
a=crypto:1 AES_CM_128_HMAC_SHA1_80 inline:...
```

---

## 6. Testing and Validation Plan

### Phase 1: Pre-Integration Testing

**Avaya Side (Before connecting to SBC):**
- [ ] Verify PSTN carrier trunk is operational
- [ ] Test internal VDN routing within Avaya
- [ ] Confirm outbound trunk to SBC is configured
- [ ] Place test call from Avaya to SBC (no Azure yet)

**SBC Side (Before connecting to Azure):**
- [ ] Verify TLS certificate is installed and valid
- [ ] Confirm public DNS resolves sbc.yourdomain.com
- [ ] Test connectivity to Azure Gov IP ranges (ping, traceroute)
- [ ] Validate SIP trunk to Avaya CM works

**Azure Side:**
- [ ] Custom domain verified in ACS
- [ ] SBC FQDN added to Direct Routing configuration
- [ ] Event Grid subscription to IVR Function created
- [ ] IVR Function deployed and healthy

### Phase 2: End-to-End Integration Testing

1. **Test Call Initiation**
   - Place call from mobile phone to DID
   - Verify CM10 receives and routes to SBC
   - Monitor SBC logs for INVITE to Azure

2. **Test SIP Signaling**
   - Capture SIP trace on SBC
   - Verify TLS handshake with Azure succeeds
   - Check for SIP 200 OK response from Azure

3. **Test Media Path**
   - Confirm RTP/SRTP media established
   - Verify caller hears IVR prompts
   - Test DTMF detection and speech recognition

4. **Test Call Control**
   - Navigate IVR menus (press digits)
   - Verify Azure Function receives events
   - Test call transfer to external number (if applicable)

5. **Failure Scenarios**
   - Disconnect Azure endpoint → CM10 fallback
   - Exceed max concurrent calls → busy treatment
   - Invalid DTMF input → reprompt logic

### Phase 3: Performance Testing

- [ ] Run 10 concurrent calls (load test)
- [ ] Monitor SBC CPU/memory utilization
- [ ] Measure call setup time (INVITE → 200 OK)
- [ ] Check for media degradation (MOS score)

---

## 7. Key Questions for Avaya Team

### A. Current Infrastructure Assessment

1. **SBC Information:**
   - What SBC model/vendor do you currently have? (Avaya SBCE, AudioCodes, Ribbon, other?)
   - What firmware version is running?
   - Is it on the [Microsoft Certified SBC List](https://docs.microsoft.com/azure/communication-services/concepts/direct-routing-infrastructure)?
   - Do you have available licensing for additional trunk capacity?

2. **CM10 Configuration:**
   - How many DIDs do we need to route to Azure IVR? (Just a few or hundreds?)
   - What's the current inbound PSTN trunk configuration? (SIP or PRI?)
   - Are there existing outbound SIP trunks configured in CM10 we can model after?
   - What's the naming convention for trunk groups and route patterns?

3. **Network Architecture:**
   - Is the SBC in the same data center as CM10?
   - Does the SBC have direct internet access, or is it behind a proxy/firewall?
   - What's the public IP address of the SBC?
   - Do you have DNS management access for creating A and TXT records?

### B. Security and Compliance

4. **Certificates:**
   - Do you have a public CA-signed certificate for the SBC FQDN?
   - When does it expire?
   - Can you generate a new certificate if needed?

5. **Access and Permissions:**
   - Who has admin access to the SBC web interface/CLI?
   - Do we need change control approval for CM10 and SBC modifications?
   - What's the maintenance window for making these changes?

### C. Operational Considerations

6. **Call Routing Logic:**
   - Should ALL calls to certain DIDs go to Azure, or do we need time-of-day routing?
   - What's the fallback strategy if Azure IVR is down? (Route to voicemail, announcement, alternate number?)
   - Do you want call recording enabled at the Avaya layer?

7. **Capacity Planning:**
   - What's the expected call volume? (Calls per hour/day?)
   - How many concurrent calls should the Azure trunk support? (10, 50, 100+?)
   - Are there peak periods we need to plan for?

8. **Support and Monitoring:**
   - How do you currently monitor SBC health and call quality?
   - What logging/tracing tools are available on the SBC?
   - Do you have SIP packet capture capability (e.g., Wireshark, Homer)?

---

## 8. Success Criteria

At the end of this integration, we should achieve:

✅ **Connectivity:**
- Inbound PSTN calls reach Avaya CM10
- CM10 successfully routes to SBC
- SBC establishes TLS connection to Azure ACS
- Azure IVR Function receives IncomingCall events

✅ **Call Quality:**
- Call setup time < 3 seconds
- Clear audio (MOS score > 3.5)
- No dropped calls during testing
- DTMF/speech recognition works reliably

✅ **Reliability:**
- 99.9% call completion rate
- Graceful fallback if Azure unavailable
- SBC high availability (if applicable)

✅ **Security:**
- All signaling encrypted (TLS 1.2+)
- All media encrypted (SRTP)
- Valid certificates with no expiration warnings

✅ **Monitoring:**
- SBC call logs visible
- Azure Application Insights capturing IVR telemetry
- Alert rules configured for failures

---

## 9. Deliverables and Next Steps

### Immediate Actions (This Meeting)

1. **Avaya Team:**
   - Confirm SBC model and firmware version
   - Share current CM10 trunk/VDN configuration (sanitized)
   - Identify DIDs to route to Azure IVR
   - Provide SBC FQDN and public IP

2. **Our Team (Azure Side):**
   - Share custom domain for Direct Routing (acs.yourdomain.com)
   - Provide Azure Government SIP endpoint details
   - Share network requirements documentation

### Post-Meeting Tasks

**Week 1:**
- [ ] Avaya: Configure outbound trunk on CM10 to SBC
- [ ] Avaya: Create VDN and vector for IVR routing
- [ ] Azure: Complete custom domain verification in ACS
- [ ] Azure: Add SBC FQDN to Direct Routing configuration

**Week 2:**
- [ ] Avaya: Configure SBC trunk to Azure (TLS, certificates)
- [ ] Avaya: Test call from CM10 to SBC (without Azure)
- [ ] Azure: Deploy IVR Function with test prompt
- [ ] Joint: Schedule end-to-end test call

**Week 3:**
- [ ] Execute Phase 2 testing plan (see Section 6)
- [ ] Troubleshoot any SIP/media issues
- [ ] Perform load testing (if required)
- [ ] Document final configuration

**Week 4:**
- [ ] Production cutover (if testing successful)
- [ ] Monitor call volume and quality for 48 hours
- [ ] Create operational runbook
- [ ] Schedule post-implementation review

---

## 10. Reference Materials

### Documentation to Share with Avaya

- [Azure Communication Services Direct Routing Overview](https://learn.microsoft.com/azure/communication-services/concepts/telephony/direct-routing-infrastructure)
- [Certified SBC List for Azure](https://learn.microsoft.com/azure/communication-services/concepts/direct-routing-infrastructure#supported-session-border-controllers)
- [SIP Signaling Requirements for Direct Routing](https://learn.microsoft.com/azure/communication-services/concepts/telephony/direct-routing-sip-requirements)
- Azure Government IP Ranges: Download from [Azure Portal](https://www.microsoft.com/download/details.aspx?id=57063)

### Internal Documentation (Our Project)

- [CM10 Setup Guide](./cm10-setup-guide.md) - Detailed Avaya configuration steps
- [ACS Configuration Guide](./acs-configuration-guide.md) - Azure setup walkthrough
- [Custom Domain Quickref](./acs-custom-domain-quickref.md) - DNS verification process
- [Architecture Overview](./architecture.md) - Full system design

---

## 11. Common Pitfalls to Avoid

⚠️ **Certificate Issues:**
- Self-signed certificates will NOT work with Azure Direct Routing
- Ensure SBC FQDN matches certificate CN/SAN exactly
- Verify entire certificate chain is valid

⚠️ **Number Format Mismatches:**
- Azure expects E.164 format: +1NXXNXXXXXX
- Avaya may use extensions or abbreviated formats
- SBC MUST normalize to E.164 before sending to Azure

⚠️ **Firewall Blocking:**
- Outbound TLS (port 5061) must be allowed to Azure IP ranges
- RTP media ports (49152-53247 UDP) must be open bidirectionally
- Don't forget to whitelist Azure Government IP ranges (different from commercial)

⚠️ **SIP Header Errors:**
- Missing P-Asserted-Identity header causes call failures
- Contact header must include SBC FQDN, not IP
- From/To headers must use correct domain (acs.yourdomain.com)

⚠️ **Media Codec Mismatch:**
- Azure only supports G.711 µ-law and a-law
- Disable HD codecs (G.722, Opus) on the Azure trunk
- Ensure no transcoding introduces latency

---

## 12. Contact Information

### Azure Team

**Technical Lead:** [Your Name]  
**Email:** [your-email]  
**Phone:** [your-phone]

**Azure Subscription ID:** 78003893-6e88-4fa2-a8f0-315067f22e79  
**ACS Resource Name:** ivr-dev-acs-bld64pwxb4ukq  
**Environment:** Azure Government Cloud  
**Region:** USGov Virginia

### Avaya Team (to be filled in)

**Avaya Engineer:** ______________________  
**Email:** ______________________  
**Phone:** ______________________  
**SBC FQDN:** ______________________  
**SBC Public IP:** ______________________

---

## Appendix: Quick Reference Checklist

Use this checklist during the meeting to ensure all points are covered:

### Configuration Items

- [x] Use case and business requirements understood
- [ ] SBC model/firmware confirmed
- [ ] SBC FQDN and public IP documented
- [ ] TLS certificate status verified
- [ ] CM10 trunk configuration approach agreed
- [ ] DID list for IVR routing provided
- [ ] Fallback strategy defined
- [ ] Network connectivity validated
- [ ] Firewall rules documented
- [ ] Testing plan and timeline agreed
- [ ] Change control process clarified
- [ ] Contact information exchanged

### Technical Validation

- [ ] SBC is Microsoft-certified for Direct Routing
- [ ] SBC has public internet access
- [ ] CM10 can route to SBC via SIP trunk
- [ ] Public DNS record for SBC exists
- [ ] Azure custom domain verification complete
- [ ] Number format normalization rules defined
- [ ] QoS/DSCP markings configured
- [ ] SIP trace/packet capture tools available

---

**Document Version:** 1.0  
**Last Updated:** June 30, 2026  
**Next Review:** After Avaya integration meeting