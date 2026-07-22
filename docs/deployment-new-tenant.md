# Deployment to New Azure Government Tenant

## Tenant Information
- **Tenant ID**: `5af05be5-b9df-43d4-8897-ec17d3118935`
- **Subscription ID**: `6dca77a5-4af1-4f7a-a47d-86ccca965104`
- **Subscription Name**: DEV
- **User**: devopsmgr@witomasidevz.onmicrosoft.us
- **Region**: usgovarizona (US Gov Arizona)

## Prerequisites Checklist
- [x] Azure CLI configured for Azure Government
- [x] Logged into correct tenant and subscription
- [ ] Azure AD App Registration created
- [ ] Parameters file updated
- [ ] Resource group created
- [ ] Infrastructure deployed
- [ ] Application code deployed

## Step-by-Step Deployment

### Step 1: Azure AD App Registration

Create an Azure AD App Registration for the Admin Portal:

```bash
# Create the app registration
az ad app create \
  --display-name "IVR Admin Portal - DEV" \
  --sign-in-audience AzureADMyOrg \
  --output json

# Note the appId from the output - this is your Client ID
```

**Save the Application (Client) ID** - you'll need it for the parameters file.

After infrastructure deployment, update the redirect URI:
```bash
# Replace <CLIENT_ID> with your app ID
# Replace <APP_SERVICE_NAME> with the deployed app service name
az ad app update \
  --id <CLIENT_ID> \
  --web-redirect-uris "https://<APP_SERVICE_NAME>.azurewebsites.us/signin-oidc"
```

### Step 2: Update Parameters File

Edit `infra/parameters/azuregov.bicepparam`:

```bicep
using '../main.bicep'

param environmentName = 'dev'
param location = 'usgovarizona'
param baseName = 'ivr'

// Update these with your new tenant/client ID
param azureAdTenantId = '5af05be5-b9df-43d4-8897-ec17d3118935'
param azureAdClientId = '<YOUR_CLIENT_ID_FROM_STEP_1>'
```

### Step 3: Create Resource Group

```bash
az group create \
  --name rg-ivr-dev \
  --location usgovarizona \
  --tags environment=dev project=ivr-system managedBy=bicep
```

### Step 4: Deploy Infrastructure

```bash
# From the repository root
az deployment group create \
  --resource-group rg-ivr-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/azuregov.bicepparam \
  --verbose
```

This deploys:
- ✅ Cosmos DB (serverless) with containers
- ✅ Storage Account for prompts and function storage
- ✅ Azure Communication Services
- ✅ Cognitive Services (Speech TTS/STT)
- ✅ Azure OpenAI (GPT-4.1)
- ✅ Function App (IVR engine)
- ✅ App Service (Admin Portal)
- ✅ App Service (PSTN Simulator)
- ✅ Application Insights

**Deployment time**: ~15-20 minutes

### Step 5: Update App Registration Redirect URI

After deployment completes, get the admin portal URL:

```bash
# Get the admin portal app name
$adminAppName = az deployment group show \
  --resource-group rg-ivr-dev \
  --name main \
  --query 'properties.outputs.adminPortalUrl.value' -o tsv

echo "Admin Portal URL: https://$adminAppName"

# Update the app registration (replace <CLIENT_ID> with your app ID)
az ad app update \
  --id <CLIENT_ID> \
  --web-redirect-uris "https://$adminAppName/signin-oidc"
```

### Step 6: Deploy Application Code

#### Option A: Using PowerShell Script (Recommended)

```powershell
# Build and publish the applications
cd src/IVR.AdminPortal
dotnet publish -c Release -o publish

# Create deployment package
Compress-Archive -Path publish\* -DestinationPath deploy.zip -Force

cd ../IVR.Functions
dotnet publish -c Release -o publish
Compress-Archive -Path publish\* -DestinationPath deploy.zip -Force

cd ../..

# Get app names from deployment
$adminAppName = az deployment group show `
  --resource-group rg-ivr-dev `
  --name main `
  --query 'properties.outputs.adminPortalUrl.value' -o tsv

$funcAppName = (az functionapp list `
  --resource-group rg-ivr-dev `
  --query '[0].name' -o tsv)

# Deploy using the script
.\deploy-apps.ps1 `
  -ResourceGroup "rg-ivr-dev" `
  -AdminAppName $adminAppName `
  -SimulatorAppName ""
```

#### Option B: Using Azure CLI

```bash
# Get the function app name
FUNC_APP_NAME=$(az functionapp list \
  --resource-group rg-ivr-dev \
  --query '[0].name' -o tsv)

# Deploy Function App
cd src/IVR.Functions
func azure functionapp publish $FUNC_APP_NAME

# Deploy Admin Portal
cd ../IVR.AdminPortal
az webapp deploy \
  --resource-group rg-ivr-dev \
  --name $ADMIN_APP_NAME \
  --src-path deploy.zip \
  --type zip
```

### Step 7: Seed Initial Data

```bash
cd src/IVR.DataSeeder

# Update appsettings.json with your Cosmos DB connection string
# (Get it from Azure Portal or deployment outputs)

dotnet run
```

This seeds:
- Sample prompts (TTS text and audio references)
- Sample menus (main menu, after-hours, VIP flow)
- Sample ANI/ALI records
- Sample team routing configuration

### Step 8: Configure Event Grid (Optional)

If using actual Azure Communication Services phone numbers:

```bash
# Get function app endpoint
FUNCTION_ENDPOINT=$(az deployment group show \
  --resource-group rg-ivr-dev \
  --name main \
  --query properties.outputs.functionAppUrl.value -o tsv)

# Get ACS resource ID
ACS_RESOURCE_ID=$(az deployment group show \
  --resource-group rg-ivr-dev \
  --name main \
  --query properties.outputs.acsResourceId.value -o tsv)

# Create Event Grid subscription for incoming calls
az eventgrid event-subscription create \
  --name ivr-incoming-calls \
  --source-resource-id $ACS_RESOURCE_ID \
  --endpoint "https://$FUNCTION_ENDPOINT/api/IncomingCallHandler" \
  --endpoint-type webhook \
  --included-event-types Microsoft.Communication.IncomingCall
```

## Post-Deployment Verification

### 1. Admin Portal Access

```bash
# Get admin portal URL
az deployment group show \
  --resource-group rg-ivr-dev \
  --name main \
  --query properties.outputs.adminPortalUrl.value -o tsv
```

Access the portal:
- URL: `https://<admin-app-name>.azurewebsites.us`
- Login with: devopsmgr@witomasidevz.onmicrosoft.us
- Verify Azure AD authentication works

### 2. PSTN Simulator Access

```bash
# Get simulator URL
az deployment group show \
  --resource-group rg-ivr-dev \
  --name main \
  --query properties.outputs.simulatorUrl.value -o tsv
```

Test the IVR:
- URL: `https://<simulator-app-name>.azurewebsites.us`
- Enter a test phone number
- Test DTMF navigation and prompts

### 3. Verify Resources

```bash
# List all deployed resources
az resource list \
  --resource-group rg-ivr-dev \
  --output table
```

Expected resources:
- Communication Services
- Cosmos DB account
- Storage account
- Cognitive Services account
- OpenAI account
- Function App
- App Service Plan
- 2x App Services (admin + simulator)
- Application Insights
- Log Analytics workspace

### 4. Check Application Insights

- Portal: https://portal.azure.us
- Navigate to Application Insights resource
- Check Live Metrics
- Verify no errors in Failures blade

## Troubleshooting

### Azure CLI Terminal Lockup
If Azure CLI commands hang (known Windows issue), use PowerShell Az module:
```powershell
Install-Module -Name Az -Scope CurrentUser -Force
Connect-AzAccount -Environment AzureUSGovernment
```

### MFA Required for App Registration
If you get MFA errors when creating app registration:
```bash
az logout
az login --tenant "5af05be5-b9df-43d4-8897-ec17d3118935" --scope "https://graph.microsoft.us//.default"
```

### OpenAI Deployment Fails
Azure OpenAI requires approval in Azure Government. If deployment fails:
1. Apply for access: https://aka.ms/oai/access
2. Wait for approval (can take several days)
3. Redeploy after approval

The system can still work without OpenAI using DTMF-based menus.

### Admin Portal Authentication Fails
1. Verify redirect URI is correctly set in App Registration
2. Verify tenant ID and client ID in App Service configuration
3. Check App Service logs for authentication errors

## Next Steps

1. **Configure Business Hours** (Admin Portal > Settings)
2. **Upload/Create Prompts** (Admin Portal > Prompts)
3. **Build Menu Trees** (Admin Portal > Call Flows)
4. **Import ANI/ALI Records** (Admin Portal > ANI/ALI or use DataSeeder)
5. **Test with PSTN Simulator**
6. **Purchase ACS Phone Numbers** (or configure Direct Routing for Avaya CM10)

## Reference Documentation

- [Azure Gov Deployment](./azure-gov-deployment.md)
- [ACS Configuration Guide](./acs-configuration-guide.md)
- [Admin Portal Guide](./admin-portal.md)
- [CM10 Setup Guide](./cm10-setup-guide.md) (for Direct Routing)
- [Architecture Overview](./architecture.md)

## Deployment Outputs

Record these for future reference:

```bash
# Save all deployment outputs
az deployment group show \
  --resource-group rg-ivr-dev \
  --name main \
  --query properties.outputs -o json > deployment-outputs.json
```

Key outputs:
- `functionAppUrl`: Function App endpoint
- `adminPortalUrl`: Admin portal URL
- `simulatorUrl`: PSTN simulator URL
- `acsResourceId`: Communication Services resource ID
