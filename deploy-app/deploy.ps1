<#
.SYNOPSIS
    Deploys the application code to Azure App Service.

.DESCRIPTION
    This script builds and deploys the .NET application to Azure App Service.
    It automatically reads deployment context from the infrastructure deployment
    so you don't need to manually specify resource names.

.PARAMETER ResourceGroup
    (Optional) The resource group name. If not specified, reads from deployment context.

.PARAMETER WebAppName
    (Optional) The web app name. If not specified, reads from deployment context.

.PARAMETER SkipBuild
    Switch to skip the build step (for quick redeployments).

.PARAMETER ConfigureSettings
    Switch to configure app settings after deployment.

.EXAMPLE
    .\deploy.ps1

.EXAMPLE
    .\deploy.ps1 -ResourceGroup "rg-myapp" -WebAppName "app-myapp"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory = $false)]
    [string]$WebAppName,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory = $false)]
    [switch]$ConfigureSettings
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Expense Management System - Application Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Try to read deployment context
$contextPath = Join-Path (Split-Path $PSScriptRoot -Parent) ".deployment-context.json"

if (Test-Path $contextPath) {
    Write-Host "Reading deployment context..." -ForegroundColor Yellow
    $context = Get-Content $contextPath | ConvertFrom-Json
    
    if ([string]::IsNullOrEmpty($ResourceGroup)) {
        $ResourceGroup = $context.resourceGroup
        Write-Host "  Resource Group: $ResourceGroup (from context)" -ForegroundColor Gray
    }
    
    if ([string]::IsNullOrEmpty($WebAppName)) {
        $WebAppName = $context.webAppName
        Write-Host "  Web App: $WebAppName (from context)" -ForegroundColor Gray
    }
    
    Write-Host ""
}

# Validate required parameters
if ([string]::IsNullOrEmpty($ResourceGroup) -or [string]::IsNullOrEmpty($WebAppName)) {
    Write-Error "ResourceGroup and WebAppName are required. Either provide them as parameters or run deploy-infra/deploy.ps1 first."
    exit 1
}

# Check Azure CLI
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    az account show --output none 2>$null
    Write-Host "✓ Logged in to Azure" -ForegroundColor Green
} catch {
    Write-Error "Not logged in to Azure. Please run: az login"
    exit 1
}

Write-Host ""

# Build application
if (-not $SkipBuild) {
    Write-Host "Building application..." -ForegroundColor Yellow
    
    $projectPath = Join-Path (Split-Path $PSScriptRoot -Parent) "src" "ExpenseManagement"
    $publishPath = Join-Path $projectPath "bin" "Release" "net8.0" "publish"
    
    Write-Host "  Project: $projectPath" -ForegroundColor Gray
    
    # Clean previous build
    if (Test-Path $publishPath) {
        Remove-Item -Path $publishPath -Recurse -Force
    }
    
    # Build and publish
    Push-Location $projectPath
    try {
        dotnet publish -c Release -o $publishPath --no-self-contained 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed"
            exit 1
        }
        
        Write-Host "✓ Build successful" -ForegroundColor Green
    } finally {
        Pop-Location
    }
    
    Write-Host ""
}

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow

$projectPath = Join-Path (Split-Path $PSScriptRoot -Parent) "src" "ExpenseManagement"
$publishPath = Join-Path $projectPath "bin" "Release" "net8.0" "publish"
$zipPath = Join-Path $PSScriptRoot "deploy.zip"

if (-not (Test-Path $publishPath)) {
    Write-Error "Publish directory not found. Run without -SkipBuild first."
    exit 1
}

# Remove old zip
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}

# Create zip with files at root level
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publishPath, $zipPath)

Write-Host "✓ Package created: $zipPath" -ForegroundColor Green
Write-Host ""

# Deploy to Azure
Write-Host "Deploying to Azure App Service..." -ForegroundColor Yellow
Write-Host "  Resource Group: $ResourceGroup" -ForegroundColor Gray
Write-Host "  Web App: $WebAppName" -ForegroundColor Gray
Write-Host ""

az webapp deploy `
    --resource-group $ResourceGroup `
    --name $WebAppName `
    --src-path $zipPath `
    --type zip `
    --clean true `
    --restart true `
    --output none

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed"
    exit 1
}

Write-Host "✓ Deployment successful" -ForegroundColor Green
Write-Host ""

# Clean up
Remove-Item -Path $zipPath -Force

# Configure settings if requested
if ($ConfigureSettings -and (Test-Path $contextPath)) {
    Write-Host "Configuring app settings..." -ForegroundColor Yellow
    
    $context = Get-Content $contextPath | ConvertFrom-Json
    
    $connectionString = "Server=tcp:$($context.sqlServerFqdn),1433;Initial Catalog=$($context.databaseName);Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Managed Identity;User Id=$($context.managedIdentityClientId);"
    
    az webapp config appsettings set `
        --name $WebAppName `
        --resource-group $ResourceGroup `
        --settings "AZURE_CLIENT_ID=$($context.managedIdentityClientId)" `
        --output none
    
    az webapp config connection-string set `
        --name $WebAppName `
        --resource-group $ResourceGroup `
        --connection-string-type SQLAzure `
        --settings DefaultConnection=$connectionString `
        --output none
    
    Write-Host "✓ Settings configured" -ForegroundColor Green
    Write-Host ""
}

# Get app URL
$appUrl = "https://$(az webapp show --resource-group $ResourceGroup --name $WebAppName --query defaultHostName -o tsv)"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Application URLs:" -ForegroundColor Yellow
Write-Host "  Main App:      $appUrl/Index" -ForegroundColor White
Write-Host "  API Docs:      $appUrl/swagger" -ForegroundColor White
Write-Host "  Health Check:  $appUrl/api/health" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Open the application in your browser" -ForegroundColor White
Write-Host "  2. Try the AI Chat interface at $appUrl/Chat" -ForegroundColor White
Write-Host "  3. Explore the API documentation at $appUrl/swagger" -ForegroundColor White
Write-Host ""
