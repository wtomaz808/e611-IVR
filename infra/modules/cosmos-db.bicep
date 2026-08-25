// ─────────────────────────────────────────────────────────────────
// Cosmos DB Account + Database + Containers for IVR System
// ─────────────────────────────────────────────────────────────────

@description('Cosmos DB account name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-02-15-preview' = {
  parent: cosmosAccount
  name: 'IvrDatabase'
  properties: {
    resource: {
      id: 'IvrDatabase'
    }
  }
}

resource aniContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'AniRecords'
  properties: {
    resource: {
      id: 'AniRecords'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [{ path: '/phoneNumber/?' }, { path: '/callerName/?' }]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

resource aliContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'AliRecords'
  properties: {
    resource: {
      id: 'AliRecords'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [{ path: '/phoneNumber/?' }, { path: '/address/city/?' }]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

resource menusContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'Menus'
  properties: {
    resource: {
      id: 'Menus'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
    }
  }
}

resource promptsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'Prompts'
  properties: {
    resource: {
      id: 'Prompts'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
    }
  }
}

resource callLogsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'CallLogs'
  properties: {
    resource: {
      id: 'CallLogs'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
      defaultTtl: 7776000 // 90 days
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          { path: '/startTime/?' }
          { path: '/callerNumber/?' }
          { path: '/status/?' }
        ]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

resource configContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'Config'
  properties: {
    resource: {
      id: 'Config'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
    }
  }
}

resource teamRoutingContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'TeamRouting'
  properties: {
    resource: {
      id: 'TeamRouting'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          { path: '/teamName/?' }
          { path: '/isActive/?' }
          { path: '/priority/?' }
        ]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

resource externalSystemsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'ExternalSystems'
  properties: {
    resource: {
      id: 'ExternalSystems'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          { path: '/systemName/?' }
          { path: '/isActive/?' }
          { path: '/systemType/?' }
        ]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

resource dataExtractionContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'DataExtraction'
  properties: {
    resource: {
      id: 'DataExtraction'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          { path: '/name/?' }
          { path: '/isActive/?' }
          { path: '/externalSystemId/?' }
        ]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

resource phoneNumbersContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'PhoneNumbers'
  properties: {
    resource: {
      id: 'PhoneNumbers'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          { path: '/phoneNumber/?' }
          { path: '/isActive/?' }
          { path: '/numberType/?' }
          { path: '/label/?' }
        ]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

resource eventSchedulesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'EventSchedules'
  properties: {
    resource: {
      id: 'EventSchedules'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          { path: '/facilityId/?' }
          { path: '/deviceId/?' }
          { path: '/isActive/?' }
          { path: '/startTimeUtc/?' }
        ]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

resource callEventsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: database
  name: 'CallEvents'
  properties: {
    resource: {
      id: 'CallEvents'
      // Partitioned by callId (not the generic /partitionKey) so events for a call are
      // co-located and RecordCallEventAsync's create-conflict idempotency check is a
      // single-partition point read.
      partitionKey: {
        paths: ['/callId']
        kind: 'Hash'
      }
      defaultTtl: 7776000 // 90 days, matches CallLogs retention
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          { path: '/callId/?' }
          { path: '/eventType/?' }
          { path: '/timestampUtc/?' }
        ]
        excludedPaths: [{ path: '/*' }]
      }
    }
  }
}

@description('Cosmos DB connection string')
@secure()
output connectionString string = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString

@description('Cosmos DB account name')
output accountName string = cosmosAccount.name
