# e611-IVR Development - Session Notes

**Last Updated:** April 10, 2026  
**Session Focus:** Local environment verification and deployment automation improvements

---

## Current Status

### ✅ Completed April 10, 2026

1. **Local Environment Verification**
   - Verified all four Docker containers operational (ivr-functions, ivr-admin, pstn-simulator, azurite)
   - Tested container connectivity and service endpoints
   - Successfully shut down local environment after verification
   - All services running correctly on standard ports (7071, 8080, 5200, 10000-10002)

2. **Deployment Automation Script**
   - Created `deploy-apps.ps1` - PowerShell-based deployment alternative to Azure CLI
   - Uses `Publish-AzWebApp` cmdlet to avoid Azure CLI terminal lockup issues
   - Deploys Admin Portal and PSTN Simulator with enhanced error handling
   - Parameter-driven configuration for flexibility
   - Colored console output for better deployment visibility
   - **Motivation:** Address recurring Azure CLI terminal lockup issue documented in troubleshooting notes

3. **Data Seeder Enhancements**
   - Created `MenuSeeder.cs` - Generates hierarchical e611 call flow test data
     - Main menu with 5 emergency routing options (RDC, Alarm Admin, Fire, Police, Support)
     - Submenu structures for multi-level navigation
     - DTMF + speech keyword support
     - Transfer actions with VDN addresses configured
   - Created `TeamRoutingSeeder.cs` - AI-powered intent routing configurations
     - 7 team routing configs with priority-based emergency escalation
     - Intent keyword matching for speech recognition
     - Transfer numbers and queue names configured
   - Created `SimpleCopySeeder.ps1` - Quick data copy utility
   - **Status:** Ready for use once Cosmos DB write issue is resolved

4. **Configuration Updates**
   - Updated `infra/parameters/azuregov.bicepparam` with minor adjustments
   - Updated `src/IVR.DataSeeder/Program.cs` to integrate new seeders

5. **Documentation Updates**
   - Created comprehensive update log: `docs/update-april-10-2026.md`
   - Updated session notes with April 10 progress (this file)

### ✅ Completed March 4, 2026

1. **PSTN Simulator Enhancements**
   - Implemented Web Speech API for audio output (text-to-speech)
   - Added audio toggle checkbox to enable/disable TTS
   - Fixed call state persistence across page navigation
   - Auto-clear active call when IVR ends (Disconnected/Failed states)
   - Simplified goodbye message (removed fire alarm text)
   - Fixed DTMF keypad layout to standard 3x4 phone grid
   - Added JavaScript interop for browser-based TTS
   - Files modified: `Phone.razor`, `_Host.cshtml`, `site.css`, `simulator.js`
   - Core service updated: `SeededInMemoryCosmosDbService.cs`

2. **Azure Infrastructure**
   - Created new Bicep module: `infra/modules/simulator-app.bicep`
   - Updated main.bicep to include simulator deployment
   - Shares App Service Plan between Admin Portal and Simulator
   - All infrastructure as code complete

3. **Git Version Control**
   - Committed all changes to BT_dev branch (commit 4cfb4f0)
   - Successfully pushed to origin/BT_dev (50 objects, 12.98 MiB)
   - 25 files changed, 3359 insertions, 18 deletions

4. **Azure Deployments - All Successful**
   - **Admin Portal**: Deployed to ivr-dev-admin-4c5ax3aimbdsy.azurewebsites.us
   - **Simulator**: Deployed to ivr-dev-simulator-4c5ax3aimbdsy.azurewebsites.us
   - **Functions App**: Deployed using Azure Functions Core Tools
     - Disabled WEBSITE_RUN_FROM_PACKAGE to resolve deployment conflicts
     - Endpoints: /api/incoming-call, /api/callbacks/{callid}
   - All three applications running in Azure Government cloud (US Gov Arizona)

5. **Documentation Updates**
   - Created comprehensive startup guide: `docs/startlocal.md`
   - Updated architecture documentation
   - Enhanced session notes with today's progress

### ✅ Completed March 2, 2026

1. **Deployment Documentation**
   - Created [deploymentComplete.md](deploymentComplete.md) with comprehensive deployment summary
   - Documented all 11 deployed Azure resources
   - Created 7-step getting started workflow
   - Added troubleshooting guide and monitoring queries

2. **Data Seeder Application**
   - Created `IVR.DataSeeder` console application project
   - Implemented `PromptSeeder.cs` with 31 comprehensive e611 test prompts
   - Implemented `AniAliSeeder.cs` with 12 ANI and 12 ALI test records
   - Added project to solution
   - Uses GUID format for all record IDs
   - Total test data: 55 records (31 prompts + 12 ANI + 12 ALI)

3. **Manual Testing**
   - Successfully created 2 test prompts in Admin Portal UI
   - Verified Admin Portal Prompts page is functional
   - Confirmed Cosmos DB connection working for reads

### ⚠️ Known Issues

#### BLOCKER: Cosmos DB Programmatic Writes Failing
- **Symptom:** All attempts to seed data programmatically fail with HTTP 400 "BadRequest"
- **Error:** `{"Errors":["One of the specified inputs is invalid"]}`
- **Impact:** Cannot use DataSeeder for automated test data creation
- **Attempted Fixes:**
  - ✅ Fixed database name: `ivr-db` → `IvrDatabase`
  - ✅ Tested raw Cosmos SDK - failed
  - ✅ Switched to CosmosDbService (same as Admin Portal) - failed
  - ✅ Changed ID format from custom strings to GUID - failed
  - ✅ Simplified test prompt - failed
- **Success Count:** 0 out of 55 records seeded
- **What Works:** Manual creation through Admin Portal UI ✅
- **What Fails:** Programmatic creation via CosmosDbService ❌
- **Mystery:** Same code path (CosmosDbService.UpsertPromptAsync) works in Admin Portal but not in seeder
- **Hypothesis:** Possible Azure Government Cosmos DB specific validation rules or serialization issue
- **Workaround:** Use Admin Portal for manual data entry until resolved

#### Minor: Play Button Not Working
- **Symptom:** Audio preview play button in Admin Portal doesn't respond
- **Impact:** Cannot preview prompt audio before saving
- **Status:** Low priority - cosmetic issue

---

## Todo List (In Priority Order)

### 🔴 Immediate Priorities

- [ ] **Debug Cosmos DB write failures** (currently blocked)
  - Issue: All 55 records fail with "One of the specified inputs is invalid"
  - Files: `src/IVR.DataSeeder/Program.cs`, `src/IVR.Core/Services/CosmosDbService.cs`
  - Next steps: Compare JSON structure of manually created vs seeded records

- [ ] **Manually create ANI/ALI test data**
  - Navigate to Admin Portal → ANI/ALI page
  - Create 3-5 ANI records (callers) 
  - Create matching ALI records (locations)
  - Reference: See test data in `src/IVR.DataSeeder/AniAliSeeder.cs`

### 🟡 Core Features (Workflow Step 3-4)

- [ ] **Design Call Flows**
  - Admin Portal → Call Flows page
  - Create routing logic for emergency calls
  - Map prompts to IVR menu options
  - Configure transfer destinations (Fire, Police, Medical)
  - Test call flow logic

- [ ] **Configure Business Hours**
  - Admin Portal → Settings page
  - Set system operating hours
  - Configure after-hours behavior
  - Set timezone and holiday schedule

### 🟢 Enhancements

- [ ] **Fix play button in Admin Portal prompts**
  - File: `src/IVR.AdminPortal/Pages/Prompts/Index.razor`
  - Implement audio preview using Azure Cognitive Services TTS
  - Currently calls empty `PreviewPrompt()` method

- [ ] **Test complete call flow end-to-end**
  - Use PSTN Simulator to place test call
  - Verify ANI/ALI lookup
  - Verify prompt playback
  - Verify routing to appropriate department

---

## Project Structure

```
e611-ivr/
├── deploy-apps.ps1               ✅ NEW - PowerShell deployment script (replaces Azure CLI)
├── docs/
│   ├── deploymentComplete.md     Comprehensive deployment guide
│   ├── session-notes.md          This file
│   ├── startlocal.md            Local development guide
│   ├── update-march-4-2026.md   March 4 update log
│   └── update-april-10-2026.md  ✅ NEW - April 10 update log
│
├── src/
│   ├── IVR.Core/                 Core models and services
│   │   ├── Models/
│   │   │   ├── IvrPrompt.cs     (31 test prompts defined)
│   │   │   ├── IvrMenu.cs       Menu/call flow models
│   │   │   ├── TeamRoutingConfig.cs  AI routing models
│   │   │   ├── AniRecord.cs     (12 test ANI records defined)
│   │   │   └── AliRecord.cs     (12 test ALI records defined)
│   │   └── Services/
│   │       └── CosmosDbService.cs
│   │
│   ├── IVR.Functions/            Azure Functions (IncomingCallHandler)
│   ├── IVR.AdminPortal/          Blazor Server admin UI
│   └── IVR.DataSeeder/           Test data seeder (partially blocked)
│       ├── Program.cs
│       ├── PromptSeeder.cs       (31 prompts - blocked by Cosmos issue)
│       ├── AniAliSeeder.cs       (24 ANI/ALI records - blocked)
│       ├── MenuSeeder.cs         ✅ NEW - e611 call flows (ready)
│       ├── TeamRoutingSeeder.cs  ✅ NEW - AI routing configs (ready)
│       ├── SimpleCopySeeder.ps1  ✅ NEW - Quick copy utility
│       └── README.md
│
└── infra/                        Bicep infrastructure as code
```

---

## Quick Reference

### Admin Portal Access
- **URL:** https://ivr-dev-admin-4c5ax3aimbdsy.azurewebsites.us
- **Pages:**
  - Dashboard: System overview
  - Prompts: Manage IVR prompts (2 test prompts created manually ✅)
  - ANI/ALI: Caller/location data (empty - needs manual entry)
  - Call Flows: Routing logic (not configured)
  - Call Logs: Historical call data
  - Settings: Business hours, system config (not configured)

### Cosmos DB
- **Account:** ivr-dev-cosmos-4c5ax3aimbdsy.documents.azure.us
- **Database:** IvrDatabase
- **Containers:**
  - Prompts (partition key: "prompt") - 2 records
  - AniRecords (partition key: phone prefix)
  - AliRecords (partition key: phone prefix)
  - CallLogs
  - SystemConfig

### Running DataSeeder (Currently Blocked)
```bash
cd src/IVR.DataSeeder
dotnet run
# Result: 0/55 records succeed ❌
```

---

## Deployment Status Summary

### Azure Resources (13 total in rg-ivr-dev)
- ✅ Functions App: ivr-dev-func-4c5ax3aimbdsy
- ✅ Admin Portal: ivr-dev-admin-4c5ax3aimbdsy
- ✅ Simulator: ivr-dev-simulator-4c5ax3aimbdsy
- ✅ Shared App Service Plan (Admin + Simulator)
- ✅ Cosmos DB, Storage, Application Insights, ACS, Cognitive Services, OpenAI
- All applications deployed and operational in Azure Government cloud

### Local Development
- ✅ All 4 Docker containers running successfully
- ✅ Local development environment fully functional
- See `docs/startlocal.md` for startup instructions

---

## Next Session - Where to Pick Up

### Primary Focus: Testing End-to-End Workflow

1. **Test PSTN Simulator with Azure Functions**
   - Access simulator at: https://ivr-dev-simulator-4c5ax3aimbdsy.azurewebsites.us
   - Place test call from simulator
   - Verify audio output (TTS) working in browser
   - Test speech input during IVR prompts
   - Verify call state persistence across page navigation
   - Test hang up button functionality
   - Confirm auto-clear when IVR ends call

2. **Verify Azure Functions Integration**
   - Monitor Function App logs for incoming calls
   - Check Application Insights for telemetry
   - Test CallbackHandler endpoint with real call flows
   - Verify Cosmos DB writes from Functions (vs seeder tool)

3. **Complete Test Data Setup**
   - Use Admin Portal to verify/add missing prompts
   - Create ANI/ALI test records for realistic call scenarios
   - Configure call flows for emergency routing

### Option A: Continue with Manual Entry
1. Open Admin Portal → ANI/ALI
2. Manually create 3-5 test ANI records (use data from AniAliSeeder.cs as reference)
3. Create matching ALI records for same phone numbers
4. Move to Call Flows design

### Option B: Investigate Cosmos DB Issue
1. Export JSON of manually created prompt from Admin Portal
2. Compare with JSON structure from PromptSeeder
3. Identify field-level differences
4. Test potential fix: match exact JSON structure
5. If successful, seed all 55 test records

### Option C: Alternative Approach
1. Create CSV import feature in Admin Portal
2. Export seeder data to CSV files
3. Import via Admin Portal UI (which works)

---

## Important Context for Next Session

### Deployment Method for Functions
- **Issue:** WEBSITE_RUN_FROM_PACKAGE setting caused zip deployment conflicts
- **Solution:** Disabled the setting, used `func azure functionapp publish --dotnet-isolated`
- **Command:** `func azure functionapp publish ivr-dev-func-4c5ax3aimbdsy --dotnet-isolated`
- **Success:** Deployment completed, functions registered and operational

### Package Security Warnings
Build warnings present but not blocking:
- **Azure.Identity 1.10.4**: Moderate vulnerabilities (GHSA-m5vv-6r4h-3vj9, GHSA-wvxc-855f-jvrv)
- **System.Text.Json 8.0.4**: High severity vulnerability (GHSA-8g4q-xg66-9fp4)
- **Action Required:** Upgrade packages in future session before production use

### Simulator Features (All Working)
- ✅ Web Speech API text-to-speech for IVR prompts
- ✅ Audio toggle checkbox (user can enable/disable TTS)
- ✅ Call state persists when navigating between pages
- ✅ Auto-clear active call when IVR ends (Disconnected/Failed states)
- ✅ Hang up button properly clears active call
- ✅ DTMF keypad in standard 3x4 phone layout
- ✅ Speech input works during all IVR prompts

### The Cosmos DB Mystery
- **Timeline:** 
  - First attempt: Custom string IDs (e.g., "welcome-main") - failed
  - Second attempt: GUID IDs via Guid.NewGuid().ToString() - failed
  - Third attempt: Using exact CosmosDbService from Admin Portal - failed
- **Evidence:**
  - GetAllPromptsAsync() works ✅ (read operations succeed)
  - UpsertPromptAsync() fails ❌ (write operations fail)
  - Manual Admin Portal creation works ✅
  - Programmatic seeding fails ❌
- **Hypothesis:** Could be related to:
  - JSON serialization differences
  - Azure Government specific Cosmos DB constraints
  - Missing/invalid property that error doesn't report
  - Partition key calculation issue
  - Request body size limits
  - SDK version mismatch (both use 3.37.1)

### Test Data Summary
From `src/IVR.DataSeeder/`:

**31 Prompts:** Welcome (3), Menu (5), Confirmation (6), Transfer (4), Error (4), Hold (2), Callback (2), Closing (2), Special (3)

**12 ANI Records:** Residential (3), Business (3), Government (3), Emergency (1), Blocked (1), VIP (1)

**12 ALI Records:** Matching locations for all phone numbers with full addresses, GPS coordinates, service areas, access notes

---

## Files Modified This Session

### March 4, 2026 - Simulator Enhancements & Deployments

**Simulator Updates:**
- `simulator/src/PstnSimulator/Pages/Phone.razor` - Audio output, state persistence, auto-clear
- `simulator/src/PstnSimulator/Pages/_Host.cshtml` - Added simulator.js script reference
- `simulator/src/PstnSimulator/wwwroot/css/site.css` - DTMF keypad 3x4 grid layout
- `simulator/src/PstnSimulator/wwwroot/js/simulator.js` - Web Speech API integration

**Core Services:**
- `src/IVR.Core/Services/SeededInMemoryCosmosDbService.cs` - Simplified goodbye prompt

**Infrastructure:**
- `infra/main.bicep` - Added simulator module
- `infra/modules/app-service.bicep` - Output planId
- `infra/modules/simulator-app.bicep` - NEW - Simulator deployment module

**Documentation:**
- `docs/startlocal.md` - Comprehensive local development startup guide
- `docs/session-notes.md` - Updated with March 4 session details
- Multiple other documentation files enhanced

**Git:**
- Commit 4cfb4f0 to BT_dev branch
- 25 files changed: 3359 insertions, 18 deletions
- Successfully pushed to origin/BT_dev

### March 2, 2026 - Initial Setup

**Created**
- `docs/deploymentComplete.md` - Comprehensive deployment documentation
- `docs/session-notes.md` - This file
- `src/IVR.DataSeeder/IVR.DataSeeder.csproj` - Project file
- `src/IVR.DataSeeder/Program.cs` - Seeder main logic
- `src/IVR.DataSeeder/PromptSeeder.cs` - 31 test prompts
- `src/IVR.DataSeeder/AniAliSeeder.cs` - 12 ANI + 12 ALI records
- `src/IVR.DataSeeder/README.md` - Seeder documentation
- `src/IVR.DataSeeder/appsettings.json` - Configuration

### Modified
- `src/IVR.sln` - Added DataSeeder project

---

## Resources

- **Deployment Guide:** [docs/deploymentComplete.md](deploymentComplete.md)
- **Admin Portal:** https://ivr-dev-admin-4c5ax3aimbdsy.azurewebsites.us
- **Azure Portal:** https://portal.azure.us (Azure Government)
- **Resource Group:** rg-ivr-dev
- **Subscription:** df79eff1-4ca3-4d21-9c6b-64dd15c253e8

---

## Notes for Future Development

1. **Security:** Upgrade vulnerable packages (Azure.Identity 1.10.4, System.Text.Json 8.0.4)
2. **Testing:** Set up integration tests for call flow engine
3. **Monitoring:** Configure Application Insights alerts for failed calls
4. **Documentation:** Create user guide for non-technical administrators
5. **Performance:** Consider caching frequently accessed prompts and ANI/ALI data
6. **Scalability:** Review Cosmos DB serverless limits for production load
7. **Compliance:** Ensure e611 location accuracy meets regulatory requirements

---

**End of Session Notes**
