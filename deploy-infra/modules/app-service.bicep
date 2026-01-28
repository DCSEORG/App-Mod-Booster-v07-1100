// App Service Module
// Creates an App Service with Standard S1 tier to avoid cold start delays

@description('The Azure region where the App Service will be created')
param location string

@description('Base name for the App Service resources')
param baseName string

@description('Resource ID of the user-assigned managed identity')
param managedIdentityId string

@description('Application Insights connection string for telemetry')
param appInsightsConnectionString string

var appServicePlanName = 'asp-${baseName}'
var webAppName = 'app-${baseName}'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'Recommended'
        }
      ]
    }
  }
}

@description('The resource ID of the App Service')
output webAppId string = webApp.id

@description('The name of the App Service')
output webAppName string = webApp.name

@description('The default hostname of the App Service')
output webAppHostName string = webApp.properties.defaultHostName

@description('The App Service Plan name')
output appServicePlanName string = appServicePlan.name
