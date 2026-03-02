# Azure Government Deployment Guide

## Overview
This guide covers deploying the E911 IVR system to **Azure Government** cloud environment.

## Azure Government Considerations

### Supported Regions
- **usgovvirginia** (US Gov Virginia)
- **usgovarizona** (US Gov Arizona) ← **This deployment**
- **usgovtexas** (US Gov Texas)

### Service Availability

| Service | Status in Azure Gov | Notes |
|---------|-------------------|-------|
| Azure Communication Services | ✅ Available | Use `dataLocation: 'unitedstates'` |
| Azure Cognitive Services (Speech) | ✅ Available | Standard SKU supported |
| Azure OpenAI | ⚠️ **Limited Availability** | Requires special approval - [Apply here](https://aka.ms/oai/access) |
| Cosmos DB | ✅ Available | Serverless mode supported |
| Blob Storage | ✅ Available | All tiers supported |
| Azure Functions | ✅ Available | v4 runtime supported |
| App Service | ✅ Available | All SKUs supported |
| Application Insights | ✅ Available | Full monitoring support |
| Event Grid | ✅ Available | Custom topics supported |

### Important Differences from Azure Public Cloud

1. **Endpoints**:
   - Portal: `https://portal.azure.us`
   - Azure CLI: Must set cloud to `AzureUSGovernment`
   - Management endpoint: `https://management.usgovcloudapi.net/`

2. **Azure OpenAI**:
   - **Requires separate approval** for Azure Government
   - May have limited model availability compared to public cloud
   - If not approved, the system can run without AI-powered routing (falls back to DTMF-based menus)

3. **Azure Communication Services**:
   - PSTN number availability may differ from public cloud
   - Direct Routing fully supported for existing phone numbers

## Prerequisites

### 1. Azure Government Access
- Active Azure Government subscription
- Appropriate permissions (Contributor or Owner role on subscription)

### 2. Azure CLI Configuration
```bash
# Set Azure CLI to use Azure Government cloud
az cloud set --name AzureUSGovernment

# Login to Azure Government
az login --tenant <your-tenant-id>

# Set your subscription
az account set --subscription <your-subscription-id>
```

### 3. Azure AD App Registration (for Admin Portal)

Create an App Registration in Azure AD for the admin portal authentication:

```bash
# Create the app registration
az ad app create \
  --display-name "IVR Admin Portal" \
  --sign-in-audience AzureADMyOrg \
  --web-redirect-uris "https://<your-app-service-name>.azurewebsites.us/signin-oidc"

# Note the Application (client) ID from the output
```

Update the `azuregov.bicepparam` file with the client ID.

### 4. Azure OpenAI Access (Optional)

If you need AI-powered routing:
1. Apply for Azure OpenAI access in Azure Government: https://aka.ms/oai/access
2. Wait for approval (can take several days)
3. Once approved, the deployment will automatically provision Azure OpenAI

If you don't have access, the system will still work with DTMF-based menus.

## Deployment Steps

### Step 1: Create Resource Group

```bash
az group create \
  --name rg-ivr-dev \
  --location usgovarizona \
  --tags environment=dev project=ivr-system managedBy=bicep
```

### Step 2: Review Parameters

Edit `infra/parameters/azuregov.bicepparam`:
- ✅ `environmentName`: Set to `dev`, `staging`, or `prod`
- ✅ `location`: Already set to `usgovarizona`
- ✅ `azureAdTenantId`: Already set
- ⚠️ `azureAdClientId`: Replace with your App Registration client ID

### Step 3: Deploy Infrastructure

```bash
az deployment group create \
  --resource-group rg-ivr-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/azuregov.bicepparam \
  --verbose
```

This will provision:
- Cosmos DB (serverless)
- Storage Account
- Azure Communication Services
- Cognitive Services (Speech)
- Azure OpenAI (if available)
- Function App
- App Service (Admin Portal)
- Application Insights

Deployment takes approximately **15-20 minutes**.

### Step 4: Configure Event Grid

After deployment, configure Event Grid to route incoming call events to your Function App:

```bash
# Get the Function App endpoint
FUNCTION_ENDPOINT=$(az deployment group show \
  --resource-group rg-ivr-dev \
  --name main \
  --query properties.outputs.functionAppUrl.value -o tsv)

# Get the ACS resource ID
ACS_RESOURCE_ID=$(az deployment group show \
  --resource-group rg-ivr-dev \
  --name main \
  --query properties.outputs.acsResourceId.value -o tsv)

# Create Event Grid subscription
az eventgrid event-subscription create \
  --name ivr-incoming-calls \
  --source-resource-id $ACS_RESOURCE_ID \
  --endpoint "https://$FUNCTION_ENDPOINT/api/IncomingCallHandler" \
  --endpoint-type webhook \
  --included-event-types Microsoft.Communication.IncomingCall
```

### Step 5: Deploy Application Code

```bash
# Deploy Function App
cd src/IVR.Functions
func azure functionapp publish <function-app-name>

# Deploy Admin Portal
cd ../IVR.AdminPortal
az webapp deploy \
  --resource-group rg-ivr-dev \
  --name <app-service-name> \
  --src-path publish.zip
```

### Step 6: Purchase or Configure Phone Numbers

**Option A: Purchase ACS Number**
```bash
az communication phonenumber purchase \
  --country-code US \
  --phone-plan-id <plan-id> \
  --communication-service-name <acs-name> \
  --resource-group rg-ivr-dev
```

**Option B: Configure Direct Routing**
See `docs/cm10-setup-guide.md` for Avaya CM10 integration via Direct Routing.

## Post-Deployment Configuration

### 1. Admin Portal Access
- URL: `https://<app-service-name>.azurewebsites.us`
- Authentication: Azure AD (using the App Registration created earlier)

### 2. Initial Setup
1. Configure business hours
2. Upload/create prompts
3. Build menu trees
4. Import ANI/ALI records
5. Configure team routing (if using AI)

## Monitoring & Troubleshooting

### Application Insights
- Portal: https://portal.azure.us
- Navigate to your Application Insights resource
- View live metrics, failures, performance data

### Common Issues

**Issue**: Azure OpenAI deployment fails  
**Solution**: Service may not be available yet. Remove OpenAI module from deployment or wait for approval.

**Issue**: Communication Services in wrong region  
**Solution**: ACS is a global service, but ensure `dataLocation` is set to `unitedstates`

**Issue**: Function App can't connect to Cosmos DB  
**Solution**: Check managed identity permissions and connection strings in App Configuration

## Security & Compliance

### FedRAMP Compliance
Azure Government is FedRAMP High authorized. Ensure your deployment meets your agency's requirements:
- Enable diagnostic logging
- Configure network security groups if needed
- Use managed identities for service-to-service authentication
- Enable Azure Policy for compliance monitoring

### Data Residency
All data remains within the United States (Arizona region).

## Cost Estimates

Approximate monthly costs for dev environment:
- Cosmos DB (serverless): $5-20
- Storage Account: $1-5
- Function App (Consumption): $5-15
- App Service (B1): $13
- Communication Services: Per-minute charges
- Cognitive Services: Per-transaction
- Azure OpenAI: Per-token (if available)

**Estimated Total**: $30-80/month + usage-based charges

## Support

For Azure Government-specific issues:
- Azure Support: https://portal.azure.us → Help + support
- Documentation: https://docs.microsoft.com/azure/azure-government/

---

**Deployment Date**: March 2, 2026  
**Target Environment**: Azure Government - Arizona Region  
**Branch**: BT_dev
