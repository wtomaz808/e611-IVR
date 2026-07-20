using '../main.bicep'

param environmentName = 'dev'
param location = 'eastus'
param baseName = 'ivr'
param azureAdTenantId = '<your-tenant-id>'
param azureAdClientId = '<your-client-id>'

// Teams Calling Bot — App Registration
// Run scripts/Create-BotAppRegistration.ps1 to generate these values.
param teamsBotAppId = '<your-bot-app-id>'
param teamsBotAppPassword = '<your-bot-app-password>'
