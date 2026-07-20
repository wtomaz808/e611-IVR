// ─────────────────────────────────────────────────────────────────
// Azure Bot Service — Teams Calling Bot Registration
// Registers the IVR calling bot and enables the Teams channel
// with voice/calling support.
//
// Prerequisites (not managed by Bicep):
//   - An Entra ID App Registration must exist before deploying this
//     module. Run scripts/Create-BotAppRegistration.ps1 first, then
//     pass the resulting App ID / Tenant ID here as parameters.
// ─────────────────────────────────────────────────────────────────

@description('Bot Service resource name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Entra App Registration Application (client) ID for the bot')
param microsoftAppId string

@description('Entra tenant ID that owns the App Registration')
param microsoftAppTenantId string

@description('Full HTTPS URL of the Function App bot messaging endpoint. Format: https://<functionapp>.azurewebsites.{suffix}/api/bot-messages')
param messagingEndpoint string

@description('Human-readable display name for the bot in Teams')
param displayName string = 'IVR Calling Bot'

@description('Bot description shown in the Teams app catalog')
param botDescription string = 'E911 IVR Teams Calling Bot — routes and manages emergency calls'

// ─── Bot Channels Registration ──────────────────────────────────
// S1 (Standard) is required for Teams calling; F0 (Free) does not
// support telephony or real-time media bots.
resource botService 'Microsoft.BotService/botServices@2022-09-15' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'S1'
  }
  kind: 'azurebot'
  properties: {
    displayName: displayName
    description: botDescription
    endpoint: messagingEndpoint
    msaAppId: microsoftAppId
    msaAppTenantId: microsoftAppTenantId
    // SingleTenant — bot and its app registration live in the same tenant.
    // Use MultiTenant only if the bot must accept calls from multiple tenants.
    msaAppType: 'SingleTenant'
    isStreamingSupported: false
    isCmekEnabled: false
    // Disable public network access if you want to lock down the bot
    // to a private endpoint later. 'Enabled' is required for Teams calling.
    publicNetworkAccess: 'Enabled'
  }
}

// ─── Microsoft Teams Channel ────────────────────────────────────
// Enables the bot to receive and control calls via Teams Phone System.
// callingWebhook is where Teams sends real-time calling notifications
// (incoming call, call connected, tone received, etc.)
resource teamsChannel 'Microsoft.BotService/botServices/channels@2022-09-15' = {
  parent: botService
  name: 'MsTeamsChannel'
  location: location
  properties: {
    channelName: 'MsTeamsChannel'
    properties: {
      enableCalling: true
      callingWebhook: messagingEndpoint
      isEnabled: true
    }
  }
}

// ─── Outputs ────────────────────────────────────────────────────
@description('Bot Service resource ID')
output resourceId string = botService.id

@description('Bot Service name — use when registering the bot in Teams Admin Center')
output botName string = botService.name

@description('Microsoft App ID — needed in Function App config and Teams Admin Center')
output microsoftAppId string = microsoftAppId

@description('Messaging endpoint the bot is registered with')
output messagingEndpoint string = messagingEndpoint
