using '../main.bicep'

// ─────────────────────────────────────────────────────────────────
// Azure Government Deployment Parameters
// Resource Group : rg-ivr-teams
// Region         : usgovvirginia (US Gov Virginia)
// Environment    : teams  (Teams calling bot — no ACS)
// Last deployed  : 2026-07-20  (ivr-teams-deploy-20260720g — Succeeded)
//
// Deployed URLs:
//   Function App  : https://ivr-teams-func-hgknk444g237w.azurewebsites.us
//   Admin Portal  : https://ivr-teams-admin-hgknk444g237w.azurewebsites.us
//   Bot Endpoint  : https://ivr-teams-func-hgknk444g237w.azurewebsites.us/api/bot-messages
//
// Bot Service: create manually in portal (ARM provider APS error in this sub)
//   portal.azure.us > Microsoft Foundry > Bot services > + Create
//   Bot handle    : ivr-teams-bot-hgknk444g237w
//   App ID        : 251948e8-7012-4fc4-a6b6-59e82c9dd983
//   Tenant ID     : 5af05be5-b9df-43d4-8897-ec17d3118935
//   Messaging URL : https://ivr-teams-func-hgknk444g237w.azurewebsites.us/api/bot-messages
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

// Route IVR Function App Graph API calls through the simulator's mock Graph endpoint
// so call control operations (answer, playPrompt, recordResponse, transfer) are
// intercepted by the simulator instead of going to the real Microsoft Graph.
param simulatorGraphEndpoint = 'https://ivr-teams-simulator-hgknk444g237w.azurewebsites.us/graph/v1.0'
