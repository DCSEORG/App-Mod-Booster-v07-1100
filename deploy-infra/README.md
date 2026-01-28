# Infrastructure Deployment

This folder contains all the infrastructure-as-code (Bicep) and deployment automation for the Expense Management System.

## What Gets Deployed

### Core Infrastructure (Always)
- ✅ User-Assigned Managed Identity
- ✅ App Service (Standard S1, Linux, .NET 8)
- ✅ Azure SQL Database (Basic tier, Entra ID auth only)
- ✅ Log Analytics Workspace
- ✅ Application Insights
- ✅ Diagnostic Settings

### Optional GenAI Resources
- 🤖 Azure OpenAI (GPT-4o model in Sweden Central)
- 🔍 Azure AI Search (Basic tier)

## Prerequisites

- **Azure CLI**: [Install](https://docs.microsoft.com/cli/azure/install-azure-cli)
- **sqlcmd** (go-sqlcmd): `winget install sqlcmd`
- **PowerShell 7+**: [Install](https://github.com/PowerShell/PowerShell/releases) (recommended)
- **Azure subscription** with permissions to create resources

## Quick Start

### 1. Login to Azure

```powershell
az login
az account set --subscription "Your Subscription Name"
```

### 2. Deploy Infrastructure

**Without GenAI:**
```powershell
.\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250115" -Location "uksouth"
```

**With GenAI:**
```powershell
.\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250115" -Location "uksouth" -DeployGenAI
```

**Parameters:**
- `-ResourceGroup`: (Required) Name for the resource group
- `-Location`: (Required) Azure region (e.g., uksouth, eastus)
- `-BaseName`: (Optional) Base name for resources (default: expensemgmt)
- `-DeployGenAI`: (Optional) Deploy Azure OpenAI and AI Search
- `-SkipDatabase`: (Optional) Skip database schema import (for redeployments)

### 3. What the Script Does

1. ✓ Validates Azure CLI and sqlcmd installation
2. ✓ Retrieves your Azure AD credentials
3. ✓ Creates resource group
4. ✓ Deploys all Bicep templates
5. ✓ Adds your IP to SQL firewall
6. ✓ Imports database schema
7. ✓ Creates managed identity database user (SID-based)
8. ✓ Deploys stored procedures
9. ✓ Configures App Service settings
10. ✓ Saves deployment context for app deployment

## Important Notes

### Resource Group Naming

Always use a **fresh resource group name** with a timestamp:
```powershell
rg-expensemgmt-20250115
rg-expensemgmt-20250115-1430
```

**Why?** Reusing resource groups with partially deployed resources can cause ARM caching issues, especially with Log Analytics Workspace references.

### Connection String Configuration

The script automatically configures these **critical** App Service settings:

```
AZURE_CLIENT_ID = <managed-identity-client-id>
ManagedIdentityClientId = <managed-identity-client-id>
ConnectionStrings__DefaultConnection = Server=tcp:...;Authentication=Active Directory Managed Identity;User Id=<client-id>;
```

Without these, the application cannot connect to the database.

### Managed Identity Authentication

- Uses **user-assigned** managed identity (not system-assigned)
- Database user created with **SID-based approach** (no Directory Reader required)
- Connection string includes `User Id` parameter for proper identity resolution

### SQL Authentication Method

- **Local/Interactive**: `ActiveDirectoryDefault`
- **CI/CD with OIDC**: `ActiveDirectoryAzCli`

The script automatically detects the environment and uses the correct method.

## File Structure

```
deploy-infra/
├── main.bicep                 # Main orchestration template
├── main.bicepparam            # Parameters file
├── deploy.ps1                 # Deployment automation script
├── modules/
│   ├── managed-identity.bicep # Managed identity resource
│   ├── app-service.bicep      # App Service and plan
│   ├── azure-sql.bicep        # SQL Server and database
│   ├── monitoring.bicep       # Log Analytics and App Insights
│   └── genai.bicep           # Azure OpenAI and AI Search (conditional)
└── README.md                  # This file
```

## Outputs

After deployment, you'll see:

```
Resource Group:    rg-expensemgmt-20250115
Web App:           https://app-expensemgmt-abc123.azurewebsites.net
SQL Server:        sql-expensemgmt-abc123.database.windows.net
Database:          Northwind
```

A deployment context file is saved at: `../.deployment-context.json`

This context file is used by the application deployment script.

## Next Steps

After infrastructure deployment completes:

1. Deploy the application code:
   ```powershell
   cd ../deploy-app
   .\deploy.ps1
   ```

2. Access the application:
   ```
   https://your-app-name.azurewebsites.net/Index
   ```

## Troubleshooting

### Issue: "sqlcmd: unrecognized argument"

**Cause:** Using legacy ODBC sqlcmd instead of go-sqlcmd.

**Solution:** 
- Install go-sqlcmd: `winget install sqlcmd`
- Restart VS Code or use standalone PowerShell terminal

### Issue: "Could not retrieve the Log Analytics workspace from ARM"

**Cause:** Resource group reuse with partial deployment.

**Solution:** Use a fresh resource group name with timestamp.

### Issue: "Login failed for user"

**Cause:** Managed identity not configured correctly in database.

**Solution:** Check that:
- The identity has been granted database roles
- Connection string includes `User Id` parameter
- `AZURE_CLIENT_ID` environment variable is set

### Issue: "Failed to create database user"

**Cause:** Directory Reader permissions issue or wrong syntax.

**Solution:** The script uses SID-based creation which doesn't require Directory Reader. Verify the managed identity client ID is correct.

## Manual Deployment (Azure CLI)

If you prefer manual deployment:

```powershell
az deployment group create \
  --resource-group rg-expensemgmt-20250115 \
  --template-file main.bicep \
  --parameters location=uksouth \
               baseName=expensemgmt \
               adminObjectId=<your-object-id> \
               adminLoginName=<your-upn> \
               deployGenAI=false
```

Note: You'll still need to manually configure database and app settings.

## CI/CD Deployment

For automated deployment with GitHub Actions, see: [`../.github/CICD-SETUP.md`](../.github/CICD-SETUP.md)

## Azure Best Practices

This deployment follows:
- ✅ [Azure SQL best practices](https://learn.microsoft.com/en-us/azure/azure-sql/database/security-best-practice)
- ✅ [App Service security best practices](https://learn.microsoft.com/en-us/azure/app-service/security-recommendations)
- ✅ [Managed Identity best practices](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/managed-identity-best-practice-recommendations)
- ✅ [Monitoring best practices](https://learn.microsoft.com/en-us/azure/azure-monitor/best-practices)

## Support

For issues or questions:
- Check the troubleshooting section above
- Review deployment logs in Azure Portal
- Check Application Insights for runtime errors
