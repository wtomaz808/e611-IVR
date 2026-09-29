using '../main.bicep'

// ─────────────────────────────────────────────────────────────────
// Azure Government Deployment Parameters
// Resource Group : rg-ivr-Mcp
// Region         : usgovvirginia (US Gov Virginia)
// Environment    : mcp  (MCP tools only — no AI agents; Teams calling deferred to Phase 8)
//
// Fully isolated stack: own Cosmos DB, Storage, Key Vault, OpenAI, Speech.
// Live call source for this phase: PSTN Simulator only.
// ─────────────────────────────────────────────────────────────────

param environmentName = 'mcp'
param location = 'usgovvirginia'
param baseName = 'ivr'

// Azure AD Configuration for Admin Portal
// App Registration: "IVR Admin Portal - MCP"
// Tenant: witomasidevz.onmicrosoft.us
param azureAdTenantId = '5af05be5-b9df-43d4-8897-ec17d3118935'
param azureAdClientId = '9fe34ecc-678c-4b44-ae0b-cbd6fca8b1d6'

// Teams Calling Bot — App Registration created for structural parity with
// rg-ivr-teams (live Teams/Phone System wiring still deferred to Phase 8;
// see docs/mcp-integration-deployment-plan.md). App Registration:
// "IVR Teams Calling Bot - MCP" (App ID below is not a secret).
param teamsBotAppId = '925bc77e-12db-4bcc-a7c0-5249c7cca2b7'
// Secret intentionally left blank here — per the plan's Infrastructure Gate
// ("no secret values in source"), pass the real value at deploy time only:
//   az deployment group create ... --parameters teamsBotAppPassword=<secret>
param teamsBotAppPassword = ''

// Bot Service ARM deployment (kind: 'sdk' — classic Bot Channels Registration)
// deploys successfully in Azure Government; verified against rg-ivr-teams's
// live bot resource (ivr-teams-bot-*, provisioningState: Succeeded). Only the
// 'azurebot' kind hits the "APS not implemented" error in Gov, and this module
// does not use it. Enabled here for parity; live Teams calling still gated to
// Phase 8 per the approved plan.
param deployBotService = true

// Simulator-first: no real Teams/Graph endpoint configured yet. Once the simulator
// is deployed, patch the Function App's GraphApiEndpoint app setting to the simulator's
// URL (see docs/mcp-integration-deployment-plan.md, Phase 6/7).
param simulatorGraphEndpoint = ''

// MCP Server — Entra-protected endpoint.
// App Registration: "IVR MCP Server API - MCP" (audience / identifier URI below)
param mcpRequireAuthentication = true
param mcpTenantId = '5af05be5-b9df-43d4-8897-ec17d3118935'
param mcpAudience = 'api://95213db6-3310-434e-9568-bf2489b8b3c7'
