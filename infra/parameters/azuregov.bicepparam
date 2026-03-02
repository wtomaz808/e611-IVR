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
// Created: 2026-03-02
param azureAdTenantId = 'd14ab12e-c535-4865-a593-c4115e7de102'
param azureAdClientId = '4e0fbbcf-a0c2-4491-84c2-bad685b028a7'
