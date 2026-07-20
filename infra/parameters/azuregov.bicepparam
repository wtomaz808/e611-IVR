using '../main.bicep'

// ─────────────────────────────────────────────────────────────────
// Azure Government Deployment Parameters
// Region: usgovarizona (Arizona)
// ─────────────────────────────────────────────────────────────────

param environmentName = 'dev'
param location = 'usgovarizona'
param baseName = 'ivr'

// Azure AD Configuration for Admin Portal
// App Registration: "IVR Admin Portal - DEV"
// Tenant: witomasidevz.onmicrosoft.us
// Created: 2026-07-10
param azureAdTenantId = '5af05be5-b9df-43d4-8897-ec17d3118935'
param azureAdClientId = '6dd15cf0-afc8-49cf-9064-163fa2f2130e'

// Teams Calling Bot — App Registration
// Run scripts/Create-BotAppRegistration.ps1 to generate these values.
// App Registration: "IVR Teams Calling Bot"
param teamsBotAppId = '<set-after-running-Create-BotAppRegistration.ps1>'
param teamsBotAppPassword = '<set-after-running-Create-BotAppRegistration.ps1>'
