using '../main.bicep'

// ─────────────────────────────────────────────────────────────────
// Azure Government Deployment Parameters
// Resource Group : rg-ivr-teams
// Region         : usgovvirginia (US Gov Virginia)
// Environment    : teams  (Teams calling bot — no ACS)
// ─────────────────────────────────────────────────────────────────

param environmentName = 'teams'
param location = 'usgovvirginia'
param baseName = 'ivr'

// Azure AD Configuration for Admin Portal
// App Registration: "IVR Admin Portal - DEV"
// Tenant: witomasidevz.onmicrosoft.us
param azureAdTenantId = '5af05be5-b9df-43d4-8897-ec17d3118935'
param azureAdClientId = '6dd15cf0-afc8-49cf-9064-163fa2f2130e'

// Teams Calling Bot — App Registration
// App Registration: "IVR Teams Calling Bot - TEAMS"
// Created: 2026-07-20  Expires: 2027-07-20
param teamsBotAppId = '251948e8-7012-4fc4-a6b6-59e82c9dd983'
// teamsBotAppPassword is injected at deploy time via --parameters teamsBotAppPassword=$botSecret
// to avoid storing the secret in source control.
param teamsBotAppPassword = ''

// Bot Service ARM deployment disabled — Gov subscription returns 'APS not implemented'.
// Create the bot manually: portal.azure.us > Microsoft Foundry > Bot services > + Create
// Then add Teams channel and set callingWebhook to the botMessagingEndpoint output.
param deployBotService = false
