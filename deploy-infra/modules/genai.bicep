// GenAI Module
// Creates Azure OpenAI and AI Search resources for the chat interface

@description('The Azure region where most resources are deployed')
param location string

@description('Base name for the GenAI resources')
param baseName string

@description('Principal ID of the managed identity to grant access')
param managedIdentityPrincipalId string

// Azure OpenAI must be in Sweden Central for better quota availability
var openAILocation = 'swedencentral'
var openAIName = toLower('oai-${baseName}-${uniqueString(resourceGroup().id)}')
var searchName = toLower('search-${baseName}-${uniqueString(resourceGroup().id)}')
var modelDeploymentName = 'gpt-4o'

resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: openAILocation
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
  }
}

resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: modelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
  }
}

resource aiSearch 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
  }
}

// Assign Cognitive Services OpenAI User role to the managed identity
var openAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, openAIUserRoleId)
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', openAIUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Assign Search Index Data Reader role to the managed identity
var searchIndexDataReaderRoleId = '1407120a-92aa-4202-b7e9-c0e197c71c8f'
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiSearch.id, managedIdentityPrincipalId, searchIndexDataReaderRoleId)
  scope: aiSearch
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataReaderRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

@description('The endpoint of the Azure OpenAI service')
output openAIEndpoint string = openAI.properties.endpoint

@description('The name of the deployed GPT model')
output openAIModelName string = modelDeploymentName

@description('The name of the Azure OpenAI resource')
output openAIName string = openAI.name

@description('The endpoint of the Azure AI Search service')
output searchEndpoint string = 'https://${aiSearch.name}.search.windows.net'

@description('The name of the Azure AI Search resource')
output searchName string = aiSearch.name
