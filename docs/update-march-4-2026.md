# System Update - March 4, 2026

## Summary

Successfully merged latest updates from main branch and deployed changes to both local development environment and Azure Government production environment.

## Changes Merged from Main

**Commit:** `6763d4c` - "update seeds" by John Spinella

### Files Updated

1. **simulator/src/PstnSimulator/appsettings.json**
   - Updated test caller data with Hawaii-based military installations
   - Replaced generic test numbers with DoD facility numbers
   - New test callers include:
     - JBPHH — HQ Pacific Fleet (+18085551234)
     - Hickam — 15th Wing HQ (+18085559876 - VIP)
     - West Loch — Magazine Ops (+18085552223)
     - NCTAMS PAC — Wahiawa (+18085554445 - VIP)
     - PMRF — Barking Sands (+18085556667)
     - Tripler Army Medical Center (+18085558889 - VIP)
     - JBPHH — Makalapa Housing (+18085553334)
     - RDC — Remote Dispatch (+18085557778 - VIP)
     - Blocked Telemarketer (+18085550001 - Blocked)

2. **Services/SeededInMemoryCosmosDbService.cs**
   - Code cleanup and refactoring (379 additions, 509 deletions)
   - Improved seed data structure and organization

## Deployments Completed

### Local Development Environment ✅

All containers rebuilt and running:

| Container | Status | Port | Updated |
|-----------|--------|------|---------|
| `ivr-functions` | ✅ Running | 7071 | No* |
| `ivr-admin` | ✅ Running | 8080 | Yes |
| `pstn-simulator` | ✅ Running | 5200 | Yes |
| `azurite` | ✅ Running | 10000-10002 | No |

*ivr-functions had no code changes in this update

### Azure Government Production ✅

Successfully deployed to Azure Government (usgovarizona):

1. **Function App Deployment**
   - Resource: `ivr-dev-func-4c5ax3aimbdsy`
   - Status: ✅ Deployed successfully
   - Method: ZIP deployment via Azure CLI

2. **Admin Portal Deployment**
   - Resource: `ivr-dev-admin-4c5ax3aimbdsy`
   - Status: ✅ RuntimeSuccessful
   - Deployment ID: `913540bc-9ac5-4be2-8373-2d06ad77a53b`
   - Method: ZIP deployment via Azure CLI

## Actions Taken

### 1. Git Operations
```powershell
git fetch origin
git stash push -m "WIP: Before merging main updates"
git merge origin/main -m "Merge main updates (update seeds)"
git stash pop
```

### 2. Local Container Updates
```powershell
docker compose up --build -d pstn-simulator ivr-admin
```

### 3. Azure Deployments

**Function App:**
```powershell
cd src/IVR.Functions
dotnet publish -c Release -o ./publish
Compress-Archive -Path * -DestinationPath deploy.zip -Force
az functionapp deployment source config-zip \
  --resource-group rg-ivr-dev \
  --name ivr-dev-func-4c5ax3aimbdsy \
  --src deploy.zip
```

**Admin Portal:**
```powershell
cd src/IVR.AdminPortal
dotnet publish -c Release -o ./publish
Compress-Archive -Path * -DestinationPath deploy.zip -Force
az webapp deployment source config-zip \
  --resource-group rg-ivr-dev \
  --name ivr-dev-admin-4c5ax3aimbdsy \
  --src deploy.zip
```

### 4. Documentation Updates

Created/updated documentation:
- ✅ **docs/startlocal.md** - Added "Updating from Main Branch" section with detailed steps
- ✅ **docs/update-march-4-2026.md** - This summary document

## Verification

### Local Environment
- ✅ All containers running and healthy
- ✅ Admin Portal accessible at http://localhost:8080
- ✅ PSTN Simulator accessible at http://localhost:5200
- ✅ Functions running at http://localhost:7071

### Azure Government
- ✅ Function App deployment successful
- ✅ Admin Portal deployment successful (RuntimeSuccessful)
- ✅ Both resources in `rg-ivr-dev` resource group
- ✅ Cloud environment: AzureUSGovernment
- ✅ Region: US Gov Arizona

## Build Warnings Noted

Both projects showed package vulnerability warnings (not blocking):
- NU1902: Azure.Identity 1.10.4 - Moderate severity vulnerabilities
  - GHSA-m5vv-6r4h-3vj9
  - GHSA-wvxc-855f-jvrv
- NU1903: System.Text.Json 8.0.4 - High severity vulnerability
  - GHSA-8g4q-xg66-9fp4

**Recommendation**: Update these packages in a future maintenance cycle.

## Current Branch Status

- **Branch**: BT_dev
- **Status**: Ahead of origin/BT_dev by 2 commits
  - Merge commit with main updates
  - Previous Team Routing UI work
- **Working Directory**: Clean (with untracked docs files)

## Next Steps

1. **Optional**: Commit documentation updates to branch
   ```powershell
   git add docs/startlocal.md docs/update-march-4-2026.md
   git commit -m "Add update documentation for March 4 main merge"
   ```

2. **Optional**: Address package vulnerabilities
   - Update Azure.Identity to latest stable version
   - Update System.Text.Json to latest patched version

3. **Testing**: Validate new seed data in PSTN Simulator
   - Test calls from military facility numbers
   - Verify VIP routing for designated numbers
   - Confirm blocked number handling

## Resources

- Main deployment docs: [docs/deploymentComplete.md](deploymentComplete.md)
- Local startup guide: [docs/startlocal.md](startlocal.md)
- Azure Gov deployment: [docs/azure-gov-deployment.md](azure-gov-deployment.md)

---

**Update Completed**: March 4, 2026, 8:05 PM UTC  
**Performed By**: GitHub Copilot  
**Environment**: Development (Azure Government)  
**Status**: ✅ All systems operational
