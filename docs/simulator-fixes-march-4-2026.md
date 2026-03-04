# Simulator Fixes & Azure Deployment - March 4, 2026

## Summary

Fixed hang up button issue in PSTN Simulator and deployed the simulator to Azure App Service in the same resource group as the rest of the system.

## Issues Fixed

### 1. Hang Up Button Not Working

**Problem:** After hanging up a call, the "Call" button remained disabled, preventing users from making additional calls without refreshing the page.

**Root Cause:** The `HandleCallCompleted` method in [simulator/src/PstnSimulator/Pages/Phone.razor](../simulator/src/PstnSimulator/Pages/Phone.razor) was not clearing the `_activeCall` variable when a call completed.

**Fix:** Added `_activeCall = null` to the `HandleCallCompleted` method:

```csharp
private void HandleCallCompleted(CallDetailRecord cdr)
{
    if (_activeCall?.Id == cdr.CallId)
    {
        _activeCall = null;  // ← Added this line
        InvokeAsync(StateHasChanged);
    }
}
```

**Impact:** Users can now hang up and immediately place a new call without page refresh.

## Azure Deployment

### Infrastructure Changes

Added PSTN Simulator as a new Azure App Service in the same resource group.

#### New Bicep Module

Created [infra/modules/simulator-app.bicep](../infra/modules/simulator-app.bicep):
- App Service for the simulator
- Shares App Service Plan with Admin Portal (cost optimization)
- Configurable ACS mode (Mock or Live)
- IVR endpoint configuration

#### Updated Bicep Files

1. **[infra/modules/app-service.bicep](../infra/modules/app-service.bicep)**
   - Added output for `planId` to share with simulator

2. **[infra/main.bicep](../infra/main.bicep)**
   - Added `simulatorApp` module deployment
   - Added `simulatorUrl` output

### Deployed Resources

| Resource | Name | Purpose |
|----------|------|---------|
| App Service Plan | ivr-dev-plan-4c5ax3aimbdsy | Shared hosting (B1 SKU) |
| Admin Portal | ivr-dev-admin-4c5ax3aimbdsy | Admin interface |
| PSTN Simulator | ivr-dev-simulator-4c5ax3aimbdsy | Call testing tool |

### Deployment Details

**Infrastructure Deployment:**
```bash
az deployment group create \
  --resource-group rg-ivr-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/azuregov.bicepparam
```

**Application Deployment:**
```bash
cd simulator/src/PstnSimulator
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..
az webapp deployment source config-zip \
  --resource-group rg-ivr-dev \
  --name ivr-dev-simulator-4c5ax3aimbdsy \
  --src deploy.zip
```

**Deployment Status:** ✅ RuntimeSuccessful

## Access URLs

### Local Development
- **PSTN Simulator**: http://localhost:5200

### Azure Government Production
- **PSTN Simulator**: https://ivr-dev-simulator-4c5ax3aimbdsy.azurewebsites.us
- **Admin Portal**: https://ivr-dev-admin-4c5ax3aimbdsy.azurewebsites.us
- **Function App**: https://ivr-dev-func-4c5ax3aimbdsy.azurewebsites.us

## Configuration

### Environment Variables (Azure Simulator)

Set in the Bicep deployment:

| Variable | Value | Description |
|----------|-------|-------------|
| `AcsMode` | Mock | Simulator runs in mock mode (no real ACS calls) |
| `IvrEndpoint` | https://ivr-dev-func-4c5ax3aimbdsy.azurewebsites.us | Function App endpoint |
| `AcsConnectionString` | (empty) | Optional for Live mode |
| `AcsCallerNumber` | (empty) | Optional for Live mode |

### Switching to Live Mode

To use real ACS (instead of mock):

```bash
az webapp config appsettings set \
  --resource-group rg-ivr-dev \
  --name ivr-dev-simulator-4c5ax3aimbdsy \
  --settings AcsMode=Live \
    AcsConnectionString="<your-acs-connection-string>" \
    AcsCallerNumber="<your-phone-number>"
```

## Testing

### Test the Hang Up Fix (Local)

1. Navigate to http://localhost:5200
2. Select a caller and DID
3. Click "Call"
4. Wait for call to connect
5. Click "Hang Up"
6. **Verify:** "Call" button becomes enabled immediately
7. Click "Call" again to place another call
8. **Expected:** New call should initiate without issues

### Test Azure Deployment

1. Navigate to https://ivr-dev-simulator-4c5ax3aimbdsy.azurewebsites.us
2. Verify the simulator loads correctly
3. Test placing a call (will use mock ACS mode)
4. Verify call flow events appear
5. Test hang up and placing another call

## Resource Group Summary

**Resource Group:** rg-ivr-dev  
**Region:** US Gov Arizona  
**Total Resources:** 12 (was 11)

New resource added:
- ✅ **ivr-dev-simulator-4c5ax3aimbdsy** - PSTN Simulator Web App

## Files Changed

### Code Changes
- [simulator/src/PstnSimulator/Pages/Phone.razor](../simulator/src/PstnSimulator/Pages/Phone.razor) - Fixed hang up button

### Infrastructure Changes
- [infra/modules/simulator-app.bicep](../infra/modules/simulator-app.bicep) - New module (created)
- [infra/modules/app-service.bicep](../infra/modules/app-service.bicep) - Added planId output
- [infra/main.bicep](../infra/main.bicep) - Added simulator module

### Documentation
- [docs/simulator-fixes-march-4-2026.md](simulator-fixes-march-4-2026.md) - This file

## Cost Impact

**Additional Monthly Cost:** ~$0  
The simulator shares the existing App Service Plan (B1) with the Admin Portal, so no additional compute costs are incurred.

## Next Steps (Optional)

1. **Update Seed Data**: The simulator now includes Hawaii military facility numbers from today's main branch merge

2. **Enable Live Mode**: If needed, configure ACS connection for real call testing

3. **CI/CD Pipeline**: Consider adding automated deployment for the simulator

4. **Monitoring**: Add Application Insights for the simulator app

## Verification Checklist

- ✅ Hang up button fix applied to local container
- ✅ Local simulator rebuilt and running
- ✅ Bicep infrastructure module created
- ✅ Infrastructure deployed to Azure Government
- ✅ Simulator app deployed to Azure
- ✅ All containers running locally
- ✅ Azure simulator accessible via HTTPS
- ✅ Documentation updated

---

**Update Completed**: March 4, 2026, 9:45 PM UTC  
**Performed By**: GitHub Copilot  
**Environment**: Development (Azure Government + Local)  
**Status**: ✅ All systems operational
