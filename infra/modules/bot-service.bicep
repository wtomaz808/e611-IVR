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

@description('Azure region for other resources in the deployment (Bot Service itself is always global)')
#disable-next-line no-unused-params
param location string

@description('Resource tags')
param tags object

@description('Entra App Registration Application (client) ID for the bot')
param microsoftAppId string

@description('Entra tenant ID (retained for azurebot kind upgrade path)')
#disable-next-line no-unused-params
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
// kind: 'sdk' = classic Bot Channels Registration — required for Azure Government.
// kind: 'azurebot' (Azure Bot) uses APS which is not implemented in Gov cloud.
resource botService 'Microsoft.BotService/botServices@2022-09-15' = {
  name: name
  location: 'global' // Microsoft.BotService is a global resource — region param is ignored
  tags: tags
  sku: {
    name: 'S1'
  }
  kind: 'sdk'
  properties: {
    displayName: displayName
    description: botDescription
    endpoint: messagingEndpoint
    msaAppId: microsoftAppId
    // Note: msaAppType and msaAppTenantId are properties of 'azurebot' kind only.
    // For 'sdk' kind, tenancy is controlled by the App Registration itself.
    isStreamingSupported: false
  }
}

// ─── Microsoft Teams Channel ────────────────────────────────────
// Enables the bot to receive and control calls via Teams Phone System.
// callingWebhook is where Teams sends real-time calling notifications
// (incoming call, call connected, tone received, etc.)
resource teamsChannel 'Microsoft.BotService/botServices/channels@2022-09-15' = {
  parent: botService
  name: 'MsTeamsChannel'
  location: 'global'
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
