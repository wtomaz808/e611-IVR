using '../main.bicep'

// ─────────────────────────────────────────────────────────────────
// Azure Government Deployment Parameters
// Subscription   : DEVDEV (dfd1f14c-0279-48b9-9b27-3be88e9f4ce1)
// Tenant         : f6bb1cfd-0389-4296-8b9c-58b772006b44
// Resource Group : rg-IVR-MCP
// Region         : usgovvirginia (US Gov Virginia)
// Environment    : mcp (MCP tools only — no AI agents)
//
// Fully isolated stack: own Cosmos DB, Storage, Key Vault, OpenAI, Speech.
// Deployed via sp-deploy (393ed379-062d-4a3d-884f-f21d98e22ecc) using
// Application Administrator / Application.ReadWrite.All Graph permission.
// ─────────────────────────────────────────────────────────────────

param environmentName = 'mcp'
param location = 'usgovvirginia'
param baseName = 'ivr'

// Azure AD Configuration for Admin Portal
// App Registration: "IVR Admin Portal - DEVDEV MCP" (fa65d660-0f36-4de3-97a6-211fab439a73)
// Redirect URI (/signin-oidc) must be patched in after first deployment,
// once the Admin Portal's actual hostname is known (depends on uniqueString()).
param azureAdTenantId = 'f6bb1cfd-0389-4296-8b9c-58b772006b44'
param azureAdClientId = 'fa65d660-0f36-4de3-97a6-211fab439a73'

// Teams Calling Bot — App Registration: "IVR Teams Calling Bot - DEVDEV MCP"
// App ID is not a secret; secret is passed at deploy time only (see deploy command).
// NOTE: The bot's Microsoft Graph calling permissions (Calls.Initiate.All, etc.)
// still require manual admin consent in the Entra admin center — Application
// Administrator / Application.ReadWrite.All cannot consent to Microsoft Graph's
// own privileged application permissions by design. Grant via:
//   entra.microsoft.us > App registrations > IVR Teams Calling Bot - DEVDEV MCP
//   > API permissions > Grant admin consent for <tenant>
param teamsBotAppId = '452972f4-c5f7-4f8c-8623-01dfa4700184'
param teamsBotAppPassword = ''

// Bot Service ARM deployment (kind: 'sdk') — deploys successfully in Azure
// Government; enabled here for structural parity with rg-ivr-Mcp / rg-ivr-teams.
param deployBotService = true

// Simulator-first: no real Teams/Graph endpoint configured yet.
param simulatorGraphEndpoint = ''

// MCP Server — Entra-protected endpoint.
// App Registration: "IVR MCP Server API - DEVDEV MCP" (52dd5caa-c12d-44aa-8333-e9da0767b5c7)
param mcpRequireAuthentication = true
param mcpTenantId = 'f6bb1cfd-0389-4296-8b9c-58b772006b44'
param mcpAudience = 'api://52dd5caa-c12d-44aa-8333-e9da0767b5c7'
