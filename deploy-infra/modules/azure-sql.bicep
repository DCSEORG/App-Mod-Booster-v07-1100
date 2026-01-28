// Azure SQL Module
// Creates Azure SQL Server and Database with Entra ID-only authentication

@description('The Azure region where the SQL resources will be created')
param location string

@description('Base name for the SQL resources')
param baseName string

@description('The Entra ID administrator Object ID')
param adminObjectId string

@description('The Entra ID administrator login name')
param adminLoginName string

@description('The Entra ID administrator principal type (User or Application)')
@allowed(['User', 'Application'])
param adminPrincipalType string = 'User'

@description('Resource ID of the user-assigned managed identity')
param managedIdentityId string

@description('Principal ID of the managed identity for server-level permissions')
param managedIdentityPrincipalId string

var sqlServerName = 'sql-${baseName}'
var databaseName = 'Northwind'

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: adminLoginName
      sid: adminObjectId
      tenantId: subscription().tenantId
      principalType: adminPrincipalType
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

@description('The fully qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The name of the SQL Server')
output sqlServerName string = sqlServer.name

@description('The name of the SQL Database')
output databaseName string = databaseName
