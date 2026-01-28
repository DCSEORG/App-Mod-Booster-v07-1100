# GitHub Actions CI/CD Setup

This document explains how to configure GitHub Actions for automated deployment using OIDC (OpenID Connect) federation.

## Prerequisites

- Azure CLI installed locally
- Owner or User Access Administrator role on the Azure subscription
- GitHub repository with this code

## Step 1: Create Service Principal with OIDC Federation

Run these commands in PowerShell:

```powershell
# Set variables
$subscriptionId = "<your-subscription-id>"
$resourceGroup = "rg-expensemgmt-prod"
$appName = "gh-expensemgmt-deploy"
$repoOwner = "<your-github-username-or-org>"
$repoName = "<your-repo-name>"

# Login to Azure
az login
az account set --subscription $subscriptionId

# Create the service principal
$spJson = az ad sp create-for-rbac --name $appName --role Owner --scopes /subscriptions/$subscriptionId --output json
$sp = $spJson | ConvertFrom-Json

# Save the client ID
$clientId = $sp.appId
Write-Host "Service Principal Client ID: $clientId"

# Create federated credential for main branch
az ad app federated-credential create `
    --id $clientId `
    --parameters "{
        \`"name\`": \`"gh-main-branch\`",
        \`"issuer\`": \`"https://token.actions.githubusercontent.com\`",
        \`"subject\`": \`"repo:${repoOwner}/${repoName}:ref:refs/heads/main\`",
        \`"audiences\`": [\`"api://AzureADTokenExchange\`"]
    }"

# Create federated credential for pull requests (optional)
az ad app federated-credential create `
    --id $clientId `
    --parameters "{
        \`"name\`": \`"gh-pull-requests\`",
        \`"issuer\`": \`"https://token.actions.githubusercontent.com\`",
        \`"subject\`": \`"repo:${repoOwner}/${repoName}:pull_request\`",
        \`"audiences\`": [\`"api://AzureADTokenExchange\`"]
    }"

Write-Host "✓ Service Principal and federated credentials created"
```

## Step 2: Assign Required Roles

The Service Principal needs two roles:

### Contributor Role (for resource management)

```powershell
az role assignment create `
    --assignee $clientId `
    --role "Contributor" `
    --scope /subscriptions/$subscriptionId
```

### User Access Administrator Role (for role assignments in Bicep)

```powershell
az role assignment create `
    --assignee $clientId `
    --role "User Access Administrator" `
    --scope /subscriptions/$subscriptionId
```

**Why User Access Administrator?** When deploying GenAI resources, the Bicep templates assign the Managed Identity access to Azure OpenAI and AI Search. Without this role, deployments will fail with:
> "The client does not have permission to perform action 'Microsoft.Authorization/roleAssignments/write'"

## Step 3: Get Azure Information

```powershell
# Get your tenant ID
$tenantId = az account show --query tenantId -o tsv

# Get your subscription ID
$subscriptionId = az account show --query id -o tsv

Write-Host "Tenant ID: $tenantId"
Write-Host "Subscription ID: $subscriptionId"
Write-Host "Client ID: $clientId"
```

## Step 4: Configure GitHub Repository

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions** → **Variables**
3. Add these repository variables (NOT secrets):

| Variable Name | Value | Example |
|--------------|-------|---------|
| `AZURE_CLIENT_ID` | Service Principal Application ID | `12345678-1234-1234-1234-123456789abc` |
| `AZURE_TENANT_ID` | Your Azure AD Tenant ID | `87654321-4321-4321-4321-cba987654321` |
| `AZURE_SUBSCRIPTION_ID` | Your Azure Subscription ID | `abcdef12-3456-7890-abcd-ef1234567890` |

**Important:** These should be configured as **Variables**, not Secrets, because they're not sensitive and need to be accessible to the workflow.

## Step 5: Create GitHub Environment (Optional but Recommended)

1. Go to **Settings** → **Environments**
2. Click **New environment**
3. Name it `production`
4. Configure protection rules:
   - ✅ Required reviewers (optional)
   - ✅ Wait timer (optional)
   - ✅ Deployment branches: **Selected branches** → Add rule for `main`

## Step 6: Update Workflow Environment Variables

Edit `.github/workflows/deploy.yml` and update these values:

```yaml
env:
  RESOURCE_GROUP: 'rg-expensemgmt-prod'  # Your resource group name
  LOCATION: 'uksouth'                     # Your Azure region
  BASE_NAME: 'expensemgmt'                # Your base name for resources
```

## Step 7: Trigger a Deployment

### Manual Deployment

1. Go to **Actions** tab in GitHub
2. Select **Deploy to Azure** workflow
3. Click **Run workflow**
4. Choose branch: `main`
5. Optionally check **Deploy GenAI resources**
6. Click **Run workflow**

### Automatic Deployment

Push changes to the `main` branch:

```bash
git add .
git commit -m "Configure CI/CD"
git push origin main
```

The workflow will automatically deploy your changes.

## Troubleshooting

### Issue: "Failed to get federated token"

**Cause:** Federated credentials not configured correctly.

**Solution:** Verify the subject in the federated credential matches your repository:
```
repo:OWNER/REPO:ref:refs/heads/main
```

### Issue: "Permission denied for roleAssignments/write"

**Cause:** Service Principal doesn't have User Access Administrator role.

**Solution:** Assign the role as shown in Step 2.

### Issue: "sqlcmd: command not found"

**Cause:** sqlcmd not installed in GitHub Actions runner.

**Solution:** The workflow includes an installation step. Verify it's not commented out.

### Issue: "Login failed for user" in database operations

**Cause:** Managed identity not configured correctly or authentication method wrong.

**Solution:** The deployment script uses `ActiveDirectoryAzCli` for CI/CD. Verify:
- The script detects `$env:GITHUB_ACTIONS`
- Uses correct authentication method
- SID-based user creation (not FROM EXTERNAL PROVIDER)

## Verify Deployment

After successful deployment:

1. Check the Actions run log for the application URL
2. Open `https://your-app.azurewebsites.net/Index`
3. Verify the application loads and connects to the database
4. Check `/api/health` endpoint
5. Explore `/swagger` for API documentation

## Security Notes

- ✅ No secrets in code - OIDC uses short-lived tokens
- ✅ Service Principal has minimum required permissions
- ✅ Federated credentials scoped to specific repository
- ✅ Environment protection rules prevent unauthorized deployments
- ✅ Managed Identity for application - no passwords in configuration

## Additional Resources

- [Azure OIDC with GitHub Actions](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure)
- [Configuring OpenID Connect in Azure](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [GitHub Actions Security Best Practices](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)
