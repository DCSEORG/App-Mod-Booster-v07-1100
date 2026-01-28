// Main Infrastructure Orchestration
// Deploys all Azure resources for the Expense Management System

@description('The Azure region where resources will be created')
param location string = resourceGroup().location

@description('Base name for all resources')
param baseName string

@description('The Entra ID administrator Object ID')
param adminObjectId string

@description('The Entra ID administrator login name')
param adminLoginName string

@description('The Entra ID administrator principal type (User for interactive, Application for CI/CD)')
@allowed(['User', 'Application'])
param adminPrincipalType string = 'User'

@description('Whether to deploy GenAI resources (Azure OpenAI and AI Search)')
param deployGenAI bool = false

@description('Timestamp for unique naming')
param timestamp string = utcNow('yyyyMMddHHmm')

// Deploy Managed Identity first as other resources depend on it
module managedIdentity 'modules/managed-identity.bicep' = {
  name: 'managedIdentity-deployment'
  params: {
    location: location
    baseName: baseName
    timestamp: timestamp
  }
}

// Deploy App Service with the managed identity
module appService 'modules/app-service.bicep' = {
  name: 'appService-deployment'
  params: {
    location: location
    baseName: baseName
    managedIdentityId: managedIdentity.outputs.managedIdentityId
    appInsightsConnectionString: '' // Will be updated after monitoring deployment
  }
}

// Deploy Azure SQL with the managed identity
module azureSQL 'modules/azure-sql.bicep' = {
  name: 'azureSQL-deployment'
  params: {
    location: location
    baseName: baseName
    adminObjectId: adminObjectId
    adminLoginName: adminLoginName
    adminPrincipalType: adminPrincipalType
    managedIdentityId: managedIdentity.outputs.managedIdentityId
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Deploy Monitoring resources
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-deployment'
  params: {
    location: location
    baseName: baseName
    webAppId: appService.outputs.webAppId
    sqlServerName: azureSQL.outputs.sqlServerName
    databaseName: azureSQL.outputs.databaseName
  }
}

// Conditionally deploy GenAI resources
module genAI 'modules/genai.bicep' = if (deployGenAI) {
  name: 'genai-deployment'
  params: {
    location: location
    baseName: baseName
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Outputs
@description('The name of the resource group')
output resourceGroupName string = resourceGroup().name

@description('The name of the App Service')
output webAppName string = appService.outputs.webAppName

@description('The hostname of the App Service')
output webAppHostName string = appService.outputs.webAppHostName

@description('The client ID of the managed identity')
output managedIdentityClientId string = managedIdentity.outputs.managedIdentityClientId

@description('The name of the managed identity')
output managedIdentityName string = managedIdentity.outputs.managedIdentityName

@description('The FQDN of the SQL Server')
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn

@description('The name of the SQL Database')
output databaseName string = azureSQL.outputs.databaseName

@description('The Application Insights connection string')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString

@description('The Azure OpenAI endpoint (empty if GenAI not deployed)')
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''

@description('The Azure OpenAI model deployment name (empty if GenAI not deployed)')
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''

@description('The Azure AI Search endpoint (empty if GenAI not deployed)')
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
