![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster

A project demonstrating how GitHub Copilot coding agent can transform screenshots of a legacy application into a modern, cloud-native Azure solution using the legacy database schema as reference.

## 💰 Expense Management System

This repository contains a complete, production-ready expense management application built with:

- ✅ **ASP.NET Core Razor Pages** (.NET 8)
- ✅ **Azure SQL Database** with Entra ID authentication
- ✅ **Azure App Service** (Standard S1)
- ✅ **Managed Identity** (passwordless authentication)
- ✅ **Application Insights** & Log Analytics monitoring
- ✅ **REST API** with Swagger documentation
- 🤖 **AI-Powered Chat** with Azure OpenAI (optional)
- 🤖 **Function Calling** for natural language operations
- 🚀 **CI/CD** with GitHub Actions and OIDC

---

## 🚀 Quick Start for App Modernization

### For Your Own Legacy App:

1. **Fork this repo**
2. **Replace the content** in:
   - `Database-Schema/database_schema.sql` - Your legacy database schema
   - `Legacy-Screenshots/` - Screenshots of your legacy UI
3. **Open GitHub Copilot Workspace**
4. **Type**: "modernise my app"
5. **Wait** for the agent to generate code (up to 30 minutes)
6. **Review and approve** the pull request
7. **Deploy to Azure**:
   ```powershell
   az login
   cd deploy-infra
   .\deploy.ps1 -ResourceGroup "rg-yourapp-$(Get-Date -Format 'yyyyMMdd')" -Location "uksouth"
   cd ../deploy-app
   .\deploy.ps1
   ```

### For the Sample Expense App:

Skip steps 2-5 and go straight to deployment! Everything is already built.

---

## ✨ Features

### Core Functionality
- ✅ **Expense Management**: Create, view, and track expenses
- ✅ **Approval Workflow**: Submit expenses for manager approval
- ✅ **Multi-Status Tracking**: Draft, Submitted, Approved, Rejected
- ✅ **Category Management**: Travel, Meals, Supplies, Accommodation, Other
- ✅ **REST API**: Full CRUD operations with Swagger documentation
- ✅ **Error Handling**: Graceful degradation with dummy data fallback

### AI-Powered Features (Optional with -DeployGenAI)
- 🤖 **Natural Language Chat**: "Show me all submitted expenses"
- 🤖 **AI Function Calling**: "Add a £50 taxi expense for today"
- 🤖 **Intelligent Actions**: Create and approve expenses via chat
- 🤖 **Conversational Queries**: Get summaries and reports

### Enterprise-Ready
- 🔐 **Passwordless Authentication**: Managed Identity with Entra ID
- 📊 **Comprehensive Monitoring**: Application Insights and Log Analytics
- 🚀 **CI/CD Pipeline**: GitHub Actions with OIDC (no secrets)
- 📘 **API Documentation**: Interactive Swagger UI
- ⚡ **High Performance**: Always-on App Service
- 🛡️ **Security Best Practices**: TLS everywhere, no passwords

---

## 📋 Prerequisites

- Azure subscription with permissions to create resources
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [sqlcmd](https://github.com/microsoft/go-sqlcmd): `winget install sqlcmd`
- [PowerShell 7+](https://github.com/PowerShell/PowerShell/releases) (recommended)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for building)

---

## 🏗️ Deployment

### 1. Deploy Infrastructure

Choose one of these options:

**Core Infrastructure Only:**
```powershell
cd deploy-infra
.\deploy.ps1 -ResourceGroup "rg-expensemgmt-$(Get-Date -Format 'yyyyMMdd')" -Location "uksouth"
```

**With AI Chat Features:**
```powershell
cd deploy-infra
.\deploy.ps1 -ResourceGroup "rg-expensemgmt-$(Get-Date -Format 'yyyyMMdd')" -Location "uksouth" -DeployGenAI
```

This deploys:
- User-Assigned Managed Identity
- App Service (S1 Standard, Linux)
- Azure SQL Database (Basic tier, Entra ID only)
- Application Insights & Log Analytics
- (Optional) Azure OpenAI + AI Search

### 2. Deploy Application

```powershell
cd ../deploy-app
.\deploy.ps1
```

The script automatically reads deployment context from step 1.

### 3. Access the Application

URLs will be displayed after deployment:

- **Main App**: `https://your-app.azurewebsites.net/Index`
- **AI Chat**: `https://your-app.azurewebsites.net/Chat`
- **API Docs**: `https://your-app.azurewebsites.net/swagger`
- **Health**: `https://your-app.azurewebsites.net/api/health`

---

## 📁 Project Structure

```
.
├── Database-Schema/
│   └── database_schema.sql           # SQL Server schema
├── Legacy-Screenshots/               # Original UI screenshots
│   ├── exp1.png (Add Expense)
│   ├── exp2.png (Approve Expenses)
│   └── exp3.png (Expenses List)
├── deploy-infra/                     # Infrastructure as Code
│   ├── main.bicep                    # Main orchestration
│   ├── deploy.ps1                    # Automated deployment
│   └── modules/                      # Bicep modules
│       ├── managed-identity.bicep
│       ├── app-service.bicep
│       ├── azure-sql.bicep
│       ├── monitoring.bicep
│       └── genai.bicep
├── deploy-app/                       # Application deployment
│   └── deploy.ps1
├── src/ExpenseManagement/            # .NET 8 application
│   ├── Models/                       # Data models
│   ├── Services/                     # Business logic
│   │   ├── DatabaseService.cs
│   │   ├── ChatService.cs
│   │   └── DummyDataService.cs
│   ├── Pages/                        # Razor Pages
│   │   ├── Index.cshtml
│   │   ├── AddExpense.cshtml
│   │   ├── ApproveExpenses.cshtml
│   │   └── Chat.cshtml
│   ├── wwwroot/                      # Static files
│   └── Program.cs                    # API endpoints
├── .github/
│   ├── workflows/deploy.yml          # CI/CD pipeline
│   └── CICD-SETUP.md                 # CI/CD setup guide
├── stored-procedures.sql             # Database stored procedures
├── ARCHITECTURE.md                   # Architecture diagrams
└── README.md                         # This file
```

---

## 🏛️ Architecture

### Technology Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | ASP.NET Core Razor Pages, JavaScript, CSS |
| **Backend** | .NET 8, C#, Minimal APIs |
| **Database** | Azure SQL Database (T-SQL, Stored Procedures) |
| **AI** | Azure OpenAI (GPT-4o), Azure AI Search |
| **Authentication** | Azure Entra ID, Managed Identity |
| **Monitoring** | Application Insights, Log Analytics |
| **Infrastructure** | Azure Bicep (IaC) |
| **CI/CD** | GitHub Actions (OIDC) |

### Azure Services Deployed

#### Core (Always)
- App Service (S1) - ~£55/month
- SQL Database (Basic) - ~£4/month  
- Managed Identity - Free
- Application Insights - Free tier
- Log Analytics - ~£2/month
- **Total: ~£61/month**

#### GenAI (Optional)
- Azure OpenAI - ~£1-5/month (usage)
- AI Search (Basic) - ~£60/month
- **Total with GenAI: ~£126/month**

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed diagrams.

---

## 🔐 Security Features

### Passwordless Authentication
- ✅ **No SQL passwords** anywhere in code or configuration
- ✅ **Managed Identity** handles all authentication automatically
- ✅ **Token-based** with automatic rotation
- ✅ **Entra ID only** SQL authentication (no username/password mode)

### Network Security
- ✅ **HTTPS only** with TLS 1.2+
- ✅ **SQL firewall** allows Azure services only
- ✅ **FTPS disabled** on App Service
- ✅ **Encrypted transit** on all connections

### Best Practices
- ✅ **No secrets in code** - all config via environment variables
- ✅ **Stored procedures only** - prevents SQL injection
- ✅ **Comprehensive logging** - audit trail for all operations
- ✅ **Health checks** - monitors application and database status

---

## 💬 AI Chat Examples

When deployed with `-DeployGenAI`, users can interact naturally:

**View Expenses:**
```
"Show me all submitted expenses"
"List expenses from last month"
"What categories are available?"
```

**Create Expenses:**
```
"Add a £50 lunch expense for today"
"Create a travel expense for £125 on January 10th"
"I need to submit a taxi receipt for £23.50"
```

**Manage Approvals:**
```
"Show me pending approvals"
"Approve expense 42"
"What expenses need my review?"
```

The AI uses **function calling** to execute real operations against the database through the REST API.

---

## 🔄 CI/CD with GitHub Actions

The repository includes a complete CI/CD pipeline using OIDC (no secrets needed!).

### Setup Steps:

1. Create Azure Service Principal with OIDC federation
2. Assign Contributor + User Access Administrator roles
3. Configure GitHub repository variables
4. Push to main branch or manually trigger workflow

See detailed instructions: [.github/CICD-SETUP.md](.github/CICD-SETUP.md)

### What Gets Automated:

- ✅ Infrastructure validation and deployment
- ✅ Database schema import
- ✅ Stored procedures deployment
- ✅ Application build and deployment
- ✅ Health check validation
- ✅ Zero secrets (OIDC authentication)

---

## 📊 API Documentation

All endpoints documented with Swagger/OpenAPI at `/swagger`

### Key Endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/expenses` | GET | List all expenses |
| `/api/expenses/status/{status}` | GET | Filter by status |
| `/api/expenses/{id}` | GET | Get single expense |
| `/api/expenses` | POST | Create new expense |
| `/api/expenses/{id}/submit` | POST | Submit for approval |
| `/api/expenses/{id}/approve` | POST | Approve expense |
| `/api/expenses/{id}/reject` | POST | Reject expense |
| `/api/categories` | GET | List categories |
| `/api/users` | GET | List users |
| `/api/chat` | POST | Chat with AI |

---

## 🐛 Troubleshooting

### Database Connection Failed

**Symptoms:** "Using Sample Data" message

**Solutions:**
1. Check `AZURE_CLIENT_ID` environment variable
2. Verify connection string includes `User Id` parameter
3. Ensure managed identity has database permissions
4. Check SQL firewall allows Azure services

### Deployment Failed

**Symptoms:** Bicep deployment errors

**Solutions:**
1. Use **fresh resource group** with timestamp
2. Verify Azure CLI login
3. Check Contributor permissions
4. Review Azure Portal deployment logs

### Chat Not Working

**Symptoms:** "Not Configured" message

**Solutions:**
1. Redeploy with `-DeployGenAI`
2. Verify OpenAI settings in App Service
3. Check managed identity has OpenAI User role

---

## 📚 Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture and diagrams
- **[deploy-infra/README.md](deploy-infra/README.md)** - Infrastructure deployment guide
- **[deploy-app/README.md](deploy-app/README.md)** - Application deployment guide
- **[.github/CICD-SETUP.md](.github/CICD-SETUP.md)** - CI/CD configuration

---

## 🎯 Use Cases

This solution demonstrates:

1. **Legacy App Modernization**: Transform old applications to cloud-native
2. **Passwordless Authentication**: Eliminate secrets and passwords
3. **Infrastructure as Code**: Reproducible deployments with Bicep
4. **AI Integration**: Add intelligent features to existing apps
5. **CI/CD Best Practices**: Automated, secure deployments
6. **Azure Best Practices**: Following Microsoft's recommended patterns

---

## 🤝 Contributing

This is a reference implementation. Feel free to:

- Fork and adapt for your own apps
- Submit issues for bugs or questions
- Suggest improvements via pull requests
- Use as a template for modernization projects

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🌟 Supporting Materials

For Microsoft Employees:
[Presentation Slides](https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs)

---

**Built with ❤️ using Azure, .NET 8, and GitHub Copilot**

**Questions?** Open an issue or check the documentation links above!
