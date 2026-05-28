using '../main.bicep'

// ─────────────────────────────────────────────────────────────────
// Azure Government Deployment Parameters
// Region: usgovarizona (Arizona)
// ─────────────────────────────────────────────────────────────────

param environmentName = 'dev'
param location = 'usgovarizona'
param baseName = 'ivr'

// Azure AD Configuration for Admin Portal
// App Registration: "IVR Admin Portal - Dev"
// Tenant: witomasidevtest.onmicrosoft.us
// Created: 2026-04-07
param azureAdTenantId = '3f6a68b1-d970-4905-863c-791674c10cf7'
param azureAdClientId = 'edea5473-795d-4feb-8228-00d3ef830b2d'
