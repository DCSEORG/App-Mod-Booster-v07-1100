<#
.SYNOPSIS
    Deploys the complete infrastructure for the Expense Management System.

.DESCRIPTION
    This script automates the deployment of all Azure resources including:
    - User-assigned Managed Identity
    - App Service with S1 Standard tier
    - Azure SQL Database with Entra ID-only authentication
    - Application Insights and Log Analytics
    - Optional: Azure OpenAI and AI Search (with -DeployGenAI switch)
    
    The script also configures database schema, stored procedures, and App Service settings.

.PARAMETER ResourceGroup
    The name of the Azure resource group to create or use.

.PARAMETER Location
    The Azure region where resources will be deployed (default: uksouth).

.PARAMETER BaseName
    Base name for all resources (default: expensemgmt). Must be lowercase.

.PARAMETER DeployGenAI
    Switch to deploy GenAI resources (Azure OpenAI and AI Search).

.PARAMETER SkipDatabase
    Switch to skip database schema import and stored procedure creation (for redeployments).

.EXAMPLE
    .\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250115" -Location "uksouth"

.EXAMPLE
    .\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250115" -Location "uksouth" -DeployGenAI
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory = $true)]
    [string]$Location,
    
    [Parameter(Mandatory = $false)]
    [string]$BaseName = "expensemgmt",
    
    [Parameter(Mandatory = $false)]
    [switch]$DeployGenAI,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipDatabase
)

$ErrorActionPreference = "Stop"

# Detect CI/CD environment
$IsCI = $env:GITHUB_ACTIONS -eq "true" -or $env:TF_BUILD -eq "true" -or $env:CI -eq "true"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Expense Management System - Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check PowerShell version
if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Warning "You are using PowerShell $($PSVersionTable.PSVersion). PowerShell 7+ is recommended."
    Write-Warning "Download from: https://github.com/PowerShell/PowerShell/releases"
}

# Validate base name is lowercase
if ($BaseName -cne $BaseName.ToLower()) {
    Write-Error "BaseName must be lowercase. Received: $BaseName"
    exit 1
}

# Check Azure CLI installation
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $azVersion = az version --output json 2>$null | ConvertFrom-Json
    Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Error "Azure CLI is not installed. Please install from: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
}

# Check if logged in to Azure
try {
    $account = az account show 2>$null | ConvertFrom-Json
    Write-Host "✓ Logged in to Azure as: $($account.user.name)" -ForegroundColor Green
    Write-Host "  Subscription: $($account.name) ($($account.id))" -ForegroundColor Gray
} catch {
    Write-Error "Not logged in to Azure. Please run: az login"
    exit 1
}

# Check sqlcmd installation
try {
    $sqlcmdVersion = sqlcmd --version 2>$null
    if ($sqlcmdVersion -match "go-sqlcmd") {
        Write-Host "✓ go-sqlcmd is installed" -ForegroundColor Green
    } else {
        Write-Warning "Legacy sqlcmd detected. Please install go-sqlcmd: winget install sqlcmd"
        Write-Warning "Or download from: https://github.com/microsoft/go-sqlcmd/releases"
    }
} catch {
    Write-Warning "sqlcmd not found. Install with: winget install sqlcmd"
    Write-Warning "Database setup will be skipped."
    $SkipDatabase = $true
}

Write-Host ""

# Get administrator credentials
Write-Host "Retrieving Azure AD credentials..." -ForegroundColor Yellow

if ($IsCI) {
    Write-Host "  Running in CI/CD mode" -ForegroundColor Gray
    
    $servicePrincipalClientId = $env:AZURE_CLIENT_ID
    if ([string]::IsNullOrEmpty($servicePrincipalClientId)) {
        Write-Error "AZURE_CLIENT_ID environment variable not set (required for CI/CD)"
        exit 1
    }
    
    $spInfo = az ad sp show --id $servicePrincipalClientId 2>$null | ConvertFrom-Json
    if ($null -eq $spInfo) {
        Write-Error "Failed to retrieve service principal information"
        exit 1
    }
    
    $adminObjectId = $spInfo.id
    $adminLoginName = $spInfo.displayName
    $adminPrincipalType = "Application"
    
    Write-Host "  Service Principal: $adminLoginName" -ForegroundColor Gray
    Write-Host "  Object ID: $adminObjectId" -ForegroundColor Gray
} else {
    Write-Host "  Running in interactive mode" -ForegroundColor Gray
    
    $currentUser = az ad signed-in-user show 2>$null | ConvertFrom-Json
    if ($null -eq $currentUser) {
        Write-Error "Failed to retrieve current user information"
        exit 1
    }
    
    $adminObjectId = $currentUser.id
    $adminLoginName = $currentUser.userPrincipalName
    $adminPrincipalType = "User"
    
    Write-Host "  User: $adminLoginName" -ForegroundColor Gray
    Write-Host "  Object ID: $adminObjectId" -ForegroundColor Gray
}

Write-Host ""

# Create or verify resource group
Write-Host "Setting up resource group: $ResourceGroup" -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroup 2>$null
if ($rgExists -eq "false") {
    Write-Host "  Creating resource group..." -ForegroundColor Gray
    az group create --name $ResourceGroup --location $Location --output none
    Write-Host "✓ Resource group created" -ForegroundColor Green
} else {
    Write-Host "✓ Resource group exists" -ForegroundColor Green
}

Write-Host ""

# Deploy Bicep templates
Write-Host "Deploying infrastructure (this may take 5-10 minutes)..." -ForegroundColor Yellow

$deploymentName = "infra-$(Get-Date -Format 'yyyyMMddHHmmss')"
$bicepFile = Join-Path $PSScriptRoot "main.bicep"

$deployParams = @(
    "--resource-group", $ResourceGroup
    "--name", $deploymentName
    "--template-file", $bicepFile
    "--parameters", "location=$Location"
    "--parameters", "baseName=$BaseName"
    "--parameters", "adminObjectId=$adminObjectId"
    "--parameters", "adminLoginName=$adminLoginName"
    "--parameters", "adminPrincipalType=$adminPrincipalType"
    "--parameters", "deployGenAI=$($DeployGenAI.IsPresent.ToString().ToLower())"
)

Write-Host "  Validating Bicep templates..." -ForegroundColor Gray
az deployment group validate @deployParams --output none 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Bicep validation failed"
    exit 1
}
Write-Host "  ✓ Validation passed" -ForegroundColor Green

Write-Host "  Deploying resources..." -ForegroundColor Gray
$deployment = az deployment group create @deployParams --output json 2>&1 | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed"
    exit 1
}

Write-Host "✓ Infrastructure deployed successfully" -ForegroundColor Green
Write-Host ""

# Extract outputs
Write-Host "Retrieving deployment outputs..." -ForegroundColor Yellow
$outputs = $deployment.properties.outputs

$webAppName = $outputs.webAppName.value
$webAppHostName = $outputs.webAppHostName.value
$managedIdentityClientId = $outputs.managedIdentityClientId.value
$managedIdentityName = $outputs.managedIdentityName.value
$sqlServerFqdn = $outputs.sqlServerFqdn.value
$databaseName = $outputs.databaseName.value
$appInsightsConnectionString = $outputs.appInsightsConnectionString.value
$openAIEndpoint = $outputs.openAIEndpoint.value
$openAIModelName = $outputs.openAIModelName.value
$searchEndpoint = $outputs.searchEndpoint.value

Write-Host "  Web App: $webAppName" -ForegroundColor Gray
Write-Host "  SQL Server: $sqlServerFqdn" -ForegroundColor Gray
Write-Host "  Managed Identity: $managedIdentityClientId" -ForegroundColor Gray
Write-Host ""

# Configure database
if (-not $SkipDatabase) {
    Write-Host "Configuring database..." -ForegroundColor Yellow
    
    # Wait for SQL Server to be ready
    Write-Host "  Waiting for SQL Server to become ready..." -ForegroundColor Gray
    Start-Sleep -Seconds 30
    
    # Add current IP to firewall
    Write-Host "  Adding current IP to firewall..." -ForegroundColor Gray
    try {
        $myIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content.Trim()
        az sql server firewall-rule create `
            --resource-group $ResourceGroup `
            --server ($sqlServerFqdn.Split('.')[0]) `
            --name "AllowMyIP" `
            --start-ip-address $myIp `
            --end-ip-address $myIp `
            --output none 2>$null
        Write-Host "  ✓ Firewall rule added for IP: $myIp" -ForegroundColor Green
    } catch {
        Write-Warning "Could not add firewall rule. You may need to add your IP manually."
    }
    
    # Import database schema
    Write-Host "  Importing database schema..." -ForegroundColor Gray
    $schemaFile = Join-Path (Split-Path $PSScriptRoot -Parent) "Database-Schema" "database_schema.sql"
    
    if (Test-Path $schemaFile) {
        $authMethod = if ($IsCI) { "ActiveDirectoryAzCli" } else { "ActiveDirectoryDefault" }
        
        $sqlcmdArgs = @(
            "-S", $sqlServerFqdn
            "-d", $databaseName
            "--authentication-method=$authMethod"
            "-i", $schemaFile
        )
        
        $output = & sqlcmd @sqlcmdArgs 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Schema imported" -ForegroundColor Green
        } else {
            Write-Warning "Schema import had issues: $output"
        }
    } else {
        Write-Warning "Schema file not found: $schemaFile"
    }
    
    # Create managed identity database user
    Write-Host "  Creating managed identity database user..." -ForegroundColor Gray
    
    # Convert Client ID to SID hex format
    $guidBytes = [System.Guid]::Parse($managedIdentityClientId).ToByteArray()
    $sidHex = "0x" + [System.BitConverter]::ToString($guidBytes).Replace("-", "")
    
    $createUserSql = @"
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = '$managedIdentityName')
    DROP USER [$managedIdentityName];

CREATE USER [$managedIdentityName] WITH SID = $sidHex, TYPE = E;

ALTER ROLE db_datareader ADD MEMBER [$managedIdentityName];
ALTER ROLE db_datawriter ADD MEMBER [$managedIdentityName];
GRANT EXECUTE TO [$managedIdentityName];
"@
    
    $sqlcmdArgs = @(
        "-S", $sqlServerFqdn
        "-d", $databaseName
        "--authentication-method=$authMethod"
        "-Q", $createUserSql
    )
    
    $output = & sqlcmd @sqlcmdArgs 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Database user created with permissions" -ForegroundColor Green
    } else {
        Write-Warning "Failed to create database user: $output"
    }
    
    # Import stored procedures
    Write-Host "  Importing stored procedures..." -ForegroundColor Gray
    $storedProcsFile = Join-Path (Split-Path $PSScriptRoot -Parent) "stored-procedures.sql"
    
    if (Test-Path $storedProcsFile) {
        $sqlcmdArgs = @(
            "-S", $sqlServerFqdn
            "-d", $databaseName
            "--authentication-method=$authMethod"
            "-i", $storedProcsFile
        )
        
        $output = & sqlcmd @sqlcmdArgs 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Stored procedures created" -ForegroundColor Green
        } else {
            Write-Warning "Stored procedure creation had issues: $output"
        }
    } else {
        Write-Warning "Stored procedures file not found: $storedProcsFile"
    }
    
    Write-Host ""
}

# Configure App Service settings
Write-Host "Configuring App Service settings..." -ForegroundColor Yellow

$connectionString = "Server=tcp:$sqlServerFqdn,1433;Initial Catalog=$databaseName;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Managed Identity;User Id=$managedIdentityClientId;"

$appSettings = @{
    "AZURE_CLIENT_ID" = $managedIdentityClientId
    "ManagedIdentityClientId" = $managedIdentityClientId
}

# Add GenAI settings if deployed
if ($DeployGenAI -and -not [string]::IsNullOrEmpty($openAIEndpoint)) {
    $appSettings["OpenAI__Endpoint"] = $openAIEndpoint
    $appSettings["OpenAI__DeploymentName"] = $openAIModelName
    $appSettings["AzureSearch__Endpoint"] = $searchEndpoint
    Write-Host "  Including GenAI settings" -ForegroundColor Gray
}

# Convert to Azure CLI format
$settingsArray = @()
foreach ($key in $appSettings.Keys) {
    $settingsArray += "$key=$($appSettings[$key])"
}

az webapp config appsettings set `
    --name $webAppName `
    --resource-group $ResourceGroup `
    --settings @settingsArray `
    --output none

az webapp config connection-string set `
    --name $webAppName `
    --resource-group $ResourceGroup `
    --connection-string-type SQLAzure `
    --settings DefaultConnection=$connectionString `
    --output none

Write-Host "✓ App Service configured" -ForegroundColor Green
Write-Host ""

# Save deployment context
Write-Host "Saving deployment context..." -ForegroundColor Yellow
$contextPath = Join-Path (Split-Path $PSScriptRoot -Parent) ".deployment-context.json"

$context = @{
    resourceGroup = $ResourceGroup
    location = $Location
    webAppName = $webAppName
    webAppHostName = $webAppHostName
    sqlServerFqdn = $sqlServerFqdn
    databaseName = $databaseName
    managedIdentityClientId = $managedIdentityClientId
    deployedAt = (Get-Date).ToString("o")
    genAIDeployed = $DeployGenAI.IsPresent
}

$context | ConvertTo-Json | Set-Content -Path $contextPath -Force
Write-Host "✓ Context saved to: $contextPath" -ForegroundColor Green
Write-Host ""

# Display summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resource Group:    $ResourceGroup" -ForegroundColor White
Write-Host "Web App:           https://$webAppHostName" -ForegroundColor White
Write-Host "SQL Server:        $sqlServerFqdn" -ForegroundColor White
Write-Host "Database:          $databaseName" -ForegroundColor White
Write-Host ""

if ($DeployGenAI -and -not [string]::IsNullOrEmpty($openAIEndpoint)) {
    Write-Host "GenAI Resources:" -ForegroundColor Yellow
    Write-Host "  Azure OpenAI:    $openAIEndpoint" -ForegroundColor White
    Write-Host "  Model:           $openAIModelName" -ForegroundColor White
    Write-Host "  AI Search:       $searchEndpoint" -ForegroundColor White
    Write-Host ""
}

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Deploy the application code:" -ForegroundColor White
Write-Host "     cd ..\deploy-app" -ForegroundColor Gray
Write-Host "     .\deploy.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Access the application:" -ForegroundColor White
Write-Host "     https://$webAppHostName/Index" -ForegroundColor Gray
Write-Host ""

if (-not $DeployGenAI) {
    Write-Host "To deploy with GenAI features, run:" -ForegroundColor Yellow
    Write-Host "  .\deploy.ps1 -ResourceGroup `"$ResourceGroup`" -Location `"$Location`" -DeployGenAI" -ForegroundColor Gray
    Write-Host ""
}
