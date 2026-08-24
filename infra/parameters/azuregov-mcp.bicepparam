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

// Teams Calling Bot — deferred to Phase 8 (simulator-first per approved plan).
// Left empty so the Function App falls back to "not configured" (no live Graph calls)
// until a real bot app registration is created for this environment.
param teamsBotAppId = ''
param teamsBotAppPassword = ''

// Bot Service ARM deployment disabled — Gov subscription returns 'APS not implemented',
// and Teams integration is deferred to Phase 8 anyway.
param deployBotService = false

// Simulator-first: no real Teams/Graph endpoint configured yet. Once the simulator
// is deployed, patch the Function App's GraphApiEndpoint app setting to the simulator's
// URL (see docs/mcp-integration-deployment-plan.md, Phase 6/7).
param simulatorGraphEndpoint = ''

// MCP Server — Entra-protected endpoint.
// App Registration: "IVR MCP Server API - MCP" (audience / identifier URI below)
param mcpRequireAuthentication = true
param mcpTenantId = '5af05be5-b9df-43d4-8897-ec17d3118935'
param mcpAudience = 'api://95213db6-3310-434e-9568-bf2489b8b3c7'
