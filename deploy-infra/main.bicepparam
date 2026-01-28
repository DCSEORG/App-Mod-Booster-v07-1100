using './main.bicep'

// Parameters for infrastructure deployment
// These values will be provided by the deployment script

param location = 'uksouth'
param baseName = 'expensemgmt'
param adminObjectId = ''
param adminLoginName = ''
param adminPrincipalType = 'User'
param deployGenAI = false
