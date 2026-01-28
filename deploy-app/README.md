# Application Deployment

This folder contains the deployment script for the Expense Management application code.

## Prerequisites

- Infrastructure must be deployed first (run `../deploy-infra/deploy.ps1`)
- Azure CLI installed and logged in
- .NET 8 SDK installed (for building)

## Quick Start

### Automatic Deployment (Recommended)

After running the infrastructure deployment, simply run:

```powershell
.\deploy.ps1
```

The script automatically reads deployment context (resource group, web app name, etc.) from the infrastructure deployment.

### Manual Deployment

If you need to specify parameters manually:

```powershell
.\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250115" -WebAppName "app-expensemgmt-abc123"
```

## What the Script Does

1. ✓ Reads deployment context from `.deployment-context.json`
2. ✓ Builds the .NET 8 application in Release mode
3. ✓ Creates deployment ZIP package (with DLL files at root)
4. ✓ Deploys to Azure App Service with clean and restart flags
5. ✓ Displays application URLs

## Parameters

- `-ResourceGroup`: (Optional) Resource group name. Auto-detected from context.
- `-WebAppName`: (Optional) Web app name. Auto-detected from context.
- `-SkipBuild`: Skip the build step (for quick redeployments)
- `-ConfigureSettings`: Configure connection string and identity settings after deployment

## ZIP Structure

The deployment ZIP must have DLL files at the **root level**, not in a subdirectory:

```
deploy.zip
├── ExpenseManagement.dll
├── appsettings.json
├── web.config
└── wwwroot/
    ├── css/
    └── js/
```

This is critical for Azure App Service to recognize the application.

## Application URLs

After deployment:

- **Main Application**: `https://your-app.azurewebsites.net/Index`
- **API Documentation**: `https://your-app.azurewebsites.net/swagger`
- **Health Check**: `https://your-app.azurewebsites.net/api/health`
- **AI Chat**: `https://your-app.azurewebsites.net/Chat`

## Configuration

### Connection String

The connection string is configured during infrastructure deployment:

```
Server=tcp:sql-server.database.windows.net,1433;
Initial Catalog=Northwind;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
Authentication=Active Directory Managed Identity;
User Id=<managed-identity-client-id>;
```

### Environment Variables

These are set by the infrastructure deployment:

```
AZURE_CLIENT_ID = <managed-identity-client-id>
ManagedIdentityClientId = <managed-identity-client-id>
APPLICATIONINSIGHTS_CONNECTION_STRING = <app-insights-connection>
```

For GenAI deployments:
```
OpenAI__Endpoint = <azure-openai-endpoint>
OpenAI__DeploymentName = gpt-4o
AzureSearch__Endpoint = <ai-search-endpoint>
```

## Local Development

To run locally:

1. Update `appsettings.Development.json` with your SQL server details
2. Use `Authentication=Active Directory Default` (not Managed Identity)
3. Run: `dotnet run --project ../../src/ExpenseManagement`

The application will start at `https://localhost:5001`

## Troubleshooting

### Issue: "Build failed"

**Cause:** .NET 8 SDK not installed or project dependencies missing.

**Solution:**
```powershell
dotnet --version  # Should show 8.x.x
cd ../../src/ExpenseManagement
dotnet restore
dotnet build
```

### Issue: "Application not starting"

**Cause:** Usually configuration issues.

**Solution:**
1. Check App Service logs in Azure Portal
2. Verify environment variables are set
3. Check Application Insights for startup errors
4. Visit `/api/health` endpoint

### Issue: "Database connection failed"

**Cause:** Connection string or managed identity not configured.

**Solution:**
Run with `-ConfigureSettings` flag:
```powershell
.\deploy.ps1 -ConfigureSettings
```

Or check the infrastructure deployment completed successfully.

### Issue: "ZIP deployment failed"

**Cause:** Azure App Service couldn't extract or recognize the package.

**Solution:**
- Verify ZIP has DLL files at root (not in subfolder)
- Check App Service deployment logs
- Try with `--clean true` flag (default in script)

## Manual Deployment

If you prefer to deploy manually:

```powershell
# Build
cd ../../src/ExpenseManagement
dotnet publish -c Release -o ./publish

# Create ZIP
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip

# Deploy
az webapp deploy \
  --resource-group rg-expensemgmt \
  --name app-expensemgmt \
  --src-path deploy.zip \
  --type zip \
  --clean true \
  --restart true
```

## Continuous Deployment

For automated deployment on every commit, see GitHub Actions workflow:
[`../.github/workflows/deploy.yml`](../.github/workflows/deploy.yml)

And setup guide:
[`../.github/CICD-SETUP.md`](../.github/CICD-SETUP.md)

## Application Features

### Core Features
- ✅ Expense listing with filtering by status
- ✅ Add new expenses
- ✅ Approve/reject pending expenses
- ✅ REST API with Swagger documentation
- ✅ Comprehensive error handling
- ✅ Dummy data fallback when database unavailable

### GenAI Features (if deployed with -DeployGenAI)
- 🤖 AI Chat assistant
- 🤖 Natural language queries
- 🤖 Function calling for database operations
- 🤖 Create expenses via chat
- 🤖 Approve expenses via chat

### API Endpoints

All endpoints are documented at `/swagger`:

- `GET /api/expenses` - List all expenses
- `GET /api/expenses/status/{status}` - Filter by status
- `GET /api/expenses/{id}` - Get single expense
- `POST /api/expenses` - Create expense
- `PUT /api/expenses` - Update expense
- `POST /api/expenses/{id}/submit` - Submit for approval
- `POST /api/expenses/{id}/approve` - Approve expense
- `POST /api/expenses/{id}/reject` - Reject expense
- `DELETE /api/expenses/{id}` - Delete expense
- `GET /api/categories` - List categories
- `GET /api/users` - List users
- `POST /api/chat` - Chat with AI assistant

## Support

For issues:
- Check Application Insights logs
- View App Service logs in Azure Portal
- Check health endpoint: `/api/health`
- Review connection strings in App Service configuration
