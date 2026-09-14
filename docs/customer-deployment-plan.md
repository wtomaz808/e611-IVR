# e611-IVR Customer Deployment Plan
## Government Organization On-Premises Integration

**Customer Type:** Government Organization (On-Premises)  
**Status:** Pre-Contract / Discovery Phase  
**Deployment Target:** Azure Government Cloud  

---

## Executive Summary

This document outlines the technical requirements, integration points, and deployment plan for integrating the e611-IVR solution with a government organization's on-premises telephony infrastructure.

**Key Requirements:**
- Azure Government tenant, subscription, and landing zone
- Network connectivity between on-premises and Azure
- Session Border Controller (SBC) deployment and configuration
- Certificate management and DNS integration
- Security compliance and governance

---

## Phase 1: Discovery & Assessment (Pre-Contract)

### 1.1 Customer Environment Discovery

**Infrastructure Assessment:**
```
□ Current Telephony System
  - Vendor/Model: _____________________ (Avaya Call Manager, Cisco, etc.)
  - Version: _____________________
  - On-premises location(s): _____________________
  - Number of sites: _____________________
  - Call volume (daily/monthly): _____________________

□ Network Infrastructure
  - Existing Azure connectivity: Yes / No
  - ExpressRoute: Yes / No (Circuit ID: _________)
  - Site-to-Site VPN: Yes / No
  - Internet egress points: _____________________
  - Firewall vendor/model: _____________________

□ DNS Infrastructure
  - DNS provider: _____________________ (Internal/On-prem, GoDaddy, Azure, etc.)
  - Domain name(s): _____________________
  - Who manages DNS: _____________________ (Team/Contact)
  - Can delegate subdomain to Azure DNS: Yes / No
  - DNS security policies (DNSSEC): Yes / No

□ Certificate Authority (CA)
  - Internal CA: Yes / No (Vendor: _________)
  - Public CA subscriptions: _____________________ (DigiCert, GoDaddy, etc.)
  - Certificate approval process: _____________________
  - Who manages certificates: _____________________ (Team/Contact)
  - Wildcard certificates available: Yes / No
  - Certificate validity period: _____________________ (1 year, 2 years)

□ Session Border Controller (SBC)
  - Existing SBC: Yes / No
  - SBC Vendor/Model: _____________________ (AudioCodes, Ribbon, Oracle, Cisco)
  - SBC location: _____________________ (On-prem, DMZ, Cloud)
  - Azure-certified SBC: Yes / No
  - Current SBC usage: _____________________ (Teams, other cloud services)
  - SBC management team: _____________________
```

### 1.2 Azure Government Readiness

**Azure Government Requirements:**
```
□ Azure Government Access
  - Organization has Azure Gov subscription: Yes / No
  - Subscription ID (if existing): _____________________
  - Subscription type: _____________________ (EA, CSP, PAYG)
  - Billing/procurement contact: _____________________

□ Landing Zone Requirements
  - Existing Azure landing zone: Yes / No
  - Hub-spoke network topology: Yes / No
  - Network security team contact: _____________________
  - Approved Azure regions: _____________________
  - Compliance requirements: _____________________ (FedRAMP, CJIS, ITAR, etc.)

□ Governance & Security
  - Azure Policy requirements: _____________________
  - Required resource tags: _____________________
  - RBAC model: _____________________
  - Security monitoring: _____________________ (Microsoft Defender, Sentinel, etc.)
  - Log retention requirements: _____________________ (90 days, 1 year, etc.)
```

### 1.3 Integration Points Assessment

**Critical Integration Questions:**
```
□ Call Routing Requirements
  - e611 call volume: _____________________ (calls/day)
  - Geographic locations: _____________________
  - ANI/ALI database: _____________________ (Existing system/provider)
  - Location identification method: _____________________ (Switch, Network, Manual)
  - PSAP routing requirements: _____________________

□ Telephony Integration
  - Direct Routing required: Yes / No
  - Existing PSTN connectivity: _____________________
  - SIP trunk provider: _____________________
  - Number portability required: Yes / No
  - Test phone numbers available: Yes / No

□ Data Integration
  - User directory (Active Directory): _____________________
  - Location database: _____________________
  - Emergency contact database: _____________________
  - Integration method: _____________________ (API, File, Database)
  - Data sensitivity/classification: _____________________
```

---

## Phase 2: Azure Foundation Setup (Post-Contract)

### 2.1 Azure Government Tenant & Subscription

**Tasks:**
1. **Establish Azure Government Tenant**
   - Register organization with Azure Government
   - Complete identity verification
   - Set up tenant administrator accounts
   - Configure Azure AD (Entra ID)

2. **Create Subscription**
   - Determine subscription type (EA, CSP, PAYG)
   - Set up billing
   - Apply cost management policies
   - Configure subscription-level RBAC

3. **Set Up Landing Zone**
   - Deploy hub-spoke network topology
   - Configure network security groups (NSGs)
   - Set up Azure Firewall or network virtual appliances
   - Implement Azure Policy for governance
   - Configure diagnostic settings and logging

### 2.2 Network Connectivity

**Option A: Azure ExpressRoute (Recommended for Production)**
```
Advantages:
- Private, dedicated connection
- Low latency, high reliability
- Predictable performance for voice traffic
- Meets government security requirements

Steps:
1. Order ExpressRoute circuit (Microsoft or partner provider)
2. Configure BGP peering
3. Set up route tables
4. Test connectivity and failover
5. Implement QoS for voice traffic

Lead Time: 4-8 weeks
Cost: $$$ (Circuit + bandwidth)
```

**Option B: Site-to-Site VPN**
```
Advantages:
- Faster deployment (days vs weeks)
- Lower initial cost
- Good for pilot/testing

Steps:
1. Deploy Azure VPN Gateway
2. Configure on-premises VPN device
3. Establish IPSec tunnel
4. Test connectivity and bandwidth
5. Configure routing

Lead Time: 1-2 weeks
Cost: $ (Gateway + bandwidth)
```

**Option C: Hybrid (ExpressRoute + VPN Backup)**
```
Best Practice for Production:
- ExpressRoute for primary
- VPN for failover
- Automatic failover configuration
```

### 2.3 DNS Strategy

**Option A: Subdomain Delegation to Azure DNS**
```
Customer DNS: contoso.gov (on-premises)
Azure DNS: azure.contoso.gov (delegated to Azure DNS)
ACS Domain: acs.azure.contoso.gov

Steps:
1. Create Azure DNS zone: azure.contoso.gov
2. Get Azure DNS nameservers
3. Customer adds NS records in contoso.gov pointing to Azure DNS
4. Manage ACS DNS records in Azure DNS (automated)

Advantages:
- Automated DNS management
- No manual coordination for changes
- Azure handles verification records

Requires:
- Customer approval to delegate subdomain
- NS record creation in customer's DNS
```

**Option B: Customer Manages All DNS**
```
Customer DNS: contoso.gov (on-premises)
ACS Domain: acs.contoso.gov

Steps:
1. Provide TXT verification records to customer
2. Customer adds records to their DNS
3. Manual coordination for any DNS changes

Advantages:
- Customer maintains full DNS control
- No changes to DNS architecture

Challenges:
- Manual process for record updates
- Requires coordination for changes
- Slower updates
```

**Recommendation:** Option A for easier management, Option B if customer has strict DNS control policies

### 2.4 Certificate Management

**Option A: Customer's Internal CA**
```
Steps:
1. Request customer to generate certificate:
   - Subject: sbc.azure.contoso.gov (or specific SBC FQDN)
   - SAN: acs.azure.contoso.gov, *.azure.contoso.gov
   - Key length: 2048-bit minimum
   - Validity: 1-2 years

2. Import customer's root CA certificate to Azure (if needed)
3. Configure SBC with certificate
4. Test TLS connectivity

Considerations:
- Internal CA must be trusted by Azure
- Certificate renewal process
- Private key management
```

**Option B: Public Certificate Authority**
```
Recommended Public CAs:
- DigiCert (preferred for government)
- GoDaddy
- Let's Encrypt (90-day validity, auto-renewal)

Steps:
1. Purchase certificate from CA
2. Generate CSR on SBC or in Azure Key Vault
3. Complete CA validation (DNS or file-based)
4. Install certificate
5. Configure auto-renewal (if supported)

Advantages:
- Universally trusted
- Standard process
- Automated renewal options
```

**Recommendation:** Public CA (DigiCert) for production unless customer has strong preference for internal CA

---

## Phase 3: SBC Deployment & Configuration

### 3.1 SBC Placement Options

**Option A: Customer On-Premises SBC**
```
Architecture:
Avaya CM → On-prem SBC → Internet → Azure ACS → Function App
           (Customer location)

Advantages:
- Customer controls SBC
- Existing SBC may be reusable
- Familiar to customer's telephony team

Requirements:
- Public IP address for SBC
- Firewall rules for SIP (port 5061)
- Media ports (UDP 49152-65535)
- TLS certificate with public FQDN
- Internet bandwidth for voice traffic

Configuration:
- Customer's network team configures SBC
- We provide Azure ACS endpoints
- Customer tests connectivity
```

**Option B: SBC in Azure (Recommended)**
```
Architecture:
Avaya CM → ExpressRoute/VPN → Azure SBC VM → Azure ACS → Function App
           (Customer)         (Azure VNet)

Advantages:
- Centralized management
- Lower latency to Azure services
- Easier integration with Azure
- We can manage SBC configuration

Requirements:
- ExpressRoute or VPN connectivity
- Azure VM for SBC (D-series recommended)
- Azure Virtual Network configuration
- Network security groups for SIP/RTP

Configuration:
- Deploy certified SBC in Azure VM
- Configure SBC for Azure ACS
- Connect to customer's Avaya via ExpressRoute/VPN
```

**Option C: Hybrid (Multi-Site)**
```
For large organizations with multiple sites:
- Regional SBCs (on-prem or Azure)
- Geographic distribution
- Redundancy and failover
```

### 3.2 SBC Configuration Requirements

**Information Needed from Customer:**
```
□ SBC Details
  - FQDN: _____________________ (e.g., sbc.contoso.gov)
  - Public IP address: _____________________
  - SIP port: _____________________ (default: 5061)
  - TLS version: _____________________ (1.2 minimum)
  - Certificate: _____________________ (installed and valid)

□ Avaya Call Manager Integration
  - Avaya CM IP address: _____________________
  - SIP trunk configuration: _____________________
  - Dial plan: _____________________
  - Call routing rules: _____________________
  - Emergency routing pattern: _____________________ (911, 933, etc.)

□ Azure ACS Endpoints
  - Provided by us after deployment:
    - ACS SIP endpoint: acs.azure.contoso.gov
    - Media endpoints: (Dynamic, firewall rules provided)
```

**SBC Configuration Steps:**
```
1. Configure SBC for Azure ACS
   - Add ACS as SIP peer
   - Configure TLS settings
   - Set up codec preferences (G.711, G.729)
   - Configure DTMF (RFC 2833)

2. Configure SBC for Avaya CM
   - Add Avaya as SIP peer
   - Configure trunk settings
   - Set up routing rules
   - Configure number translation

3. Test Connectivity
   - SBC to Azure ACS: SIP OPTIONS
   - SBC to Avaya: SIP OPTIONS
   - End-to-end test call
   - Verify media flow (RTP)
```

---

## Phase 4: IVR Deployment to Azure

### 4.1 Azure Resources Deployment

**Infrastructure as Code (Bicep/Terraform):**
```powershell
# Deploy all Azure resources
cd C:\DSOP\repos3\IVR\e611-ivr\infra

# Update parameters for customer environment
# Edit parameters/production.bicepparam with:
# - Correct Azure Gov region
# - Customer naming conventions
# - Network configuration
# - Security settings

# Deploy infrastructure
az deployment sub create \
  --location usgovvirginia \
  --template-file main.bicep \
  --parameters production.bicepparam
```

**Resources Deployed:**
- Azure Communication Services (ACS)
- Azure Functions (IVR logic)
- Azure Cosmos DB (call data, location database)
- Azure Storage (call recordings, logs)
- Azure Application Insights (monitoring)
- Azure Key Vault (secrets, certificates)
- Networking (VNet, Private Endpoints if required)

### 4.2 Security Configuration

**Network Security:**
```
□ Private Endpoints (if required by customer)
  - ACS private endpoint
  - Storage private endpoint
  - Cosmos DB private endpoint
  - Function App VNet integration

□ Firewall Rules
  - NSG rules for Function App
  - Azure Firewall policies
  - Allow SBC → Azure communication
  - Allow Function App → External services

□ DDoS Protection
  - Enable Azure DDoS Protection Standard
  - Configure alerts
```

**Identity & Access:**
```
□ Managed Identities
  - Function App managed identity
  - Access to Key Vault
  - Access to Cosmos DB
  - Access to Storage

□ RBAC Assignments
  - Deployment service principals
  - Operations team access
  - Customer administrator access
  - Read-only monitoring access

□ Key Vault Configuration
  - Store ACS connection string
  - Store Cosmos DB keys
  - Store certificates
  - Configure access policies
  - Enable soft delete and purge protection
```

---

## Phase 5: Integration & Testing

### 5.1 Integration Testing Plan

**Test Scenarios:**
```
1. Basic Call Flow
   □ Incoming call received
   □ IVR prompts play correctly
   □ DTMF input recognized
   □ Call routed to correct PSAP

2. Location Identification
   □ ANI lookup successful
   □ Location database query
   □ Correct address retrieved
   □ Location sent to PSAP

3. Failover & Redundancy
   □ Primary SBC failure
   □ Network connectivity loss
   □ Azure region failover (if multi-region)
   □ Database failover

4. Call Quality
   □ Audio clarity
   □ Latency < 150ms
   □ No dropped packets
   □ DTMF reliability

5. Compliance & Logging
   □ Call recordings stored
   □ Audit logs captured
   □ PII handling compliant
   □ Retention policies enforced
```

### 5.2 User Acceptance Testing (UAT)

**Customer Validation:**
```
□ Test with customer's telephony team
□ Verify call routing
□ Validate location accuracy
□ Confirm PSAP integration
□ Review reporting and analytics
□ Security and compliance review
□ Performance validation
```

---

## Phase 6: Production Cutover

### 6.1 Cutover Plan

**Pre-Cutover Checklist:**
```
□ All testing completed and passed
□ Customer sign-off on UAT
□ Runbooks and documentation complete
□ Support team trained
□ Monitoring and alerts configured
□ Backup and recovery tested
□ Rollback plan documented
```

**Cutover Steps:**
```
1. Schedule maintenance window
2. Communicate to stakeholders
3. Configure production SBC
4. Update Avaya routing
5. Enable production traffic
6. Monitor initial calls
7. Validate functionality
8. Document issues (if any)
9. Confirm success criteria
10. Close maintenance window
```

### 6.2 Post-Cutover Support

**Hypercare Period (30 days):**
```
- 24/7 monitoring
- Dedicated support team
- Daily status meetings
- Issue tracking and resolution
- Performance tuning
- Customer feedback collection
```

---

## Risk Assessment & Mitigation

### Technical Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Customer network connectivity issues | High | Medium | ExpressRoute + VPN backup, thorough network testing |
| SBC configuration complexity | Medium | Medium | Use certified SBC, vendor support, thorough testing |
| Certificate management delays | Medium | High | Start certificate process early, use public CA |
| DNS coordination delays | Low | Medium | Plan DNS changes in advance, use delegation if possible |
| Azure Gov feature availability | Medium | Low | Validate all features in Azure Gov before contract |
| Call quality issues | High | Low | QoS configuration, bandwidth monitoring |

### Operational Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Customer approval delays | Medium | High | Clear decision points, regular stakeholder meetings |
| Multiple customer teams (network, security, telephony) | Medium | High | Central customer PM, regular coordination meetings |
| Compliance requirements | High | Medium | Early compliance review, involve security team |
| Training and knowledge transfer | Medium | Medium | Comprehensive documentation, hands-on training |

---

## Timeline Estimate

**Optimistic (Best Case):**
```
Week 1-2:   Discovery & Assessment
Week 3-4:   Azure tenant and subscription setup
Week 5-6:   Network connectivity (VPN)
Week 7-8:   DNS and certificate setup
Week 9-10:  SBC configuration
Week 11-12: IVR deployment and integration
Week 13-14: Testing and UAT
Week 15:    Production cutover
─────────────────────────────────
Total: 15-16 weeks (~4 months)
```

**Realistic (Expected):**
```
Week 1-4:   Discovery, approvals, procurement
Week 5-8:   Azure foundation setup
Week 9-12:  Network connectivity (ExpressRoute lead time)
Week 13-16: DNS, certificates, governance
Week 17-20: SBC deployment and configuration
Week 21-24: IVR deployment
Week 25-28: Integration testing
Week 29-32: UAT and fixes
Week 33-34: Production cutover
─────────────────────────────────
Total: 34 weeks (~8 months)
```

**Conservative (Worst Case):**
```
Considerations:
- Government procurement delays: +8-12 weeks
- Security/compliance reviews: +4-8 weeks
- Multi-site deployment: +8-12 weeks
- Complex integrations: +4-8 weeks
─────────────────────────────────
Total: 12-18 months
```

---

## Success Criteria

**Technical:**
- [ ] 99.9% uptime for IVR service
- [ ] Call answer within 2 seconds
- [ ] < 150ms latency for call setup
- [ ] 100% accurate location identification
- [ ] All e611 calls routed correctly

**Operational:**
- [ ] Customer telephony team trained
- [ ] Support runbooks complete
- [ ] Monitoring and alerting functional
- [ ] Backup and recovery tested
- [ ] Compliance requirements met

**Business:**
- [ ] Customer satisfaction
- [ ] Meets regulatory requirements
- [ ] Within budget and timeline
- [ ] Scalable for future growth

---

## Next Steps (Immediate Actions)

1. **Review this document** with your team
2. **Schedule discovery session** with customer (post-contract)
3. **Prepare discovery questionnaire** (customized for customer)
4. **Identify key customer contacts:**
   - Project manager/sponsor
   - Telephony/Avaya team
   - Network team
   - Security team
   - DNS/certificate team
5. **Validate Azure Government feature availability** for all required services
6. **Prepare statement of work (SOW)** with timeline and deliverables

---

## Appendices

### Appendix A: Contact Information Template

```
Customer Contacts:
- Project Sponsor: _____________________
- Technical Lead: _____________________
- Telephony Team: _____________________
- Network Team: _____________________
- Security Team: _____________________
- DNS Administrator: _____________________

Microsoft/Partner Contacts:
- Project Manager: _____________________
- Solution Architect: _____________________
- Deployment Engineer: _____________________
- Support Team: _____________________
```

### Appendix B: Useful Resources

- Azure Government Documentation: https://docs.microsoft.com/azure/azure-government/
- ACS Direct Routing: https://docs.microsoft.com/azure/communication-services/concepts/telephony-sip
- Certified SBCs: https://docs.microsoft.com/azure/communication-services/concepts/telephony-sip#session-border-controllers-sbcs
- Azure Landing Zones: https://docs.microsoft.com/azure/cloud-adoption-framework/ready/landing-zone/
- Azure Government Compliance: https://docs.microsoft.com/azure/azure-government/compliance/

### Appendix C: Decision Log Template

| Date | Decision | Rationale | Decision Maker | Impact |
|------|----------|-----------|----------------|--------|
| | | | | |

---

**Document Version:** 1.0  
**Last Updated:** May 28, 2026  
**Next Review:** After customer discovery session
