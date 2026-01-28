# Expense Management System - Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           AZURE SUBSCRIPTION                                 │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                        Resource Group                                   │ │
│  │                                                                         │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐  │ │
│  │  │                     User-Assigned Managed Identity                │  │ │
│  │  │                  (Passwordless Authentication)                    │  │ │
│  │  └────────────┬──────────────────────────────┬─────────────────────┘  │ │
│  │               │                               │                         │ │
│  │               │                               │                         │ │
│  │  ┌────────────▼───────────────┐    ┌────────▼────────────────┐        │ │
│  │  │      App Service (S1)      │    │   Azure SQL Database    │        │ │
│  │  │   ┌──────────────────┐     │    │   ┌────────────────┐   │        │ │
│  │  │   │  .NET 8 Web App  │     │    │   │   Northwind    │   │        │ │
│  │  │   │                  │     │    │   │   Database     │   │        │ │
│  │  │   │ - Razor Pages    │     │    │   │                │   │        │ │
│  │  │   │ - REST API       │───────────────▶│ Stored Procs  │   │        │ │
│  │  │   │ - Swagger UI     │     │    │   │ Entra ID Auth  │   │        │ │
│  │  │   │ - Chat UI        │     │    │   │ (No Passwords) │   │        │ │
│  │  │   └──────────────────┘     │    │   └────────────────┘   │        │ │
│  │  │                             │    │                         │        │ │
│  │  │   Environment Variables:   │    │   Basic Tier            │        │ │
│  │  │   - AZURE_CLIENT_ID        │    │                         │        │ │
│  │  │   - ConnectionStrings__    │    └─────────────────────────┘        │ │
│  │  │     DefaultConnection      │                                        │ │
│  │  └────────────┬───────────────┘                                        │ │
│  │               │                                                         │ │
│  │               │                                                         │ │
│  │  ┌────────────▼───────────────────────────────────────────┐            │ │
│  │  │              Application Insights                       │            │ │
│  │  │         ┌──────────────────────────┐                   │            │ │
│  │  │         │  Log Analytics Workspace │                   │            │ │
│  │  │         │  - Application logs      │                   │            │ │
│  │  │         │  - SQL diagnostics       │                   │            │ │
│  │  │         │  - Performance metrics   │                   │            │ │
│  │  │         └──────────────────────────┘                   │            │ │
│  │  └────────────────────────────────────────────────────────┘            │ │
│  │                                                                         │ │
│  │  ┌────────────────────────────────────────────────────────┐            │ │
│  │  │              GenAI Resources (Optional)                │            │ │
│  │  │                                                         │            │ │
│  │  │  ┌────────────────────┐      ┌────────────────────┐   │            │ │
│  │  │  │  Azure OpenAI      │      │   Azure AI Search  │   │            │ │
│  │  │  │  (Sweden Central)  │      │                    │   │            │ │
│  │  │  │                    │      │   Basic Tier       │   │            │ │
│  │  │  │  Model: GPT-4o     │      │                    │   │            │ │
│  │  │  │  Capacity: 8       │      │   For RAG          │   │            │ │
│  │  │  └────────────────────┘      └────────────────────┘   │            │ │
│  │  │                                                         │            │ │
│  │  │  Accessed by Managed Identity                          │            │ │
│  │  │  Role: Cognitive Services OpenAI User                  │            │ │
│  │  └────────────────────────────────────────────────────────┘            │ │
│  │                                                                         │ │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                          EXTERNAL USERS                                      │
│                                                                              │
│  ┌──────────────┐     HTTPS      ┌──────────────────────┐                  │
│  │   Browser    │◀───────────────▶│   App Service        │                  │
│  │              │                  │   Public Endpoint    │                  │
│  │ - /Index     │                  │                      │                  │
│  │ - /AddExpense│                  │   TLS 1.2+          │                  │
│  │ - /Approve   │                  │   HTTPS Only        │                  │
│  │ - /Chat      │                  └──────────────────────┘                  │
│  │ - /swagger   │                                                            │
│  └──────────────┘                                                            │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Authentication Flow

### Application ⟷ SQL Database

```
┌──────────────────┐
│   App Service    │
│                  │
│ AZURE_CLIENT_ID  │
│  = abc123...     │
└────────┬─────────┘
         │
         │ 1. Request token from Azure AD
         │    using Managed Identity
         ▼
┌──────────────────┐
│   Azure AD       │
│                  │
│  Issues token    │
└────────┬─────────┘
         │
         │ 2. Token with identity claims
         ▼
┌──────────────────┐
│  SQL Database    │
│                  │
│  Validates token │
│  Checks database │
│  user exists     │
│                  │
│  Grants access   │
└──────────────────┘
```

### Application ⟷ Azure OpenAI

```
┌──────────────────┐
│   App Service    │
│                  │
│ ManagedIdentity  │
│ Credential       │
└────────┬─────────┘
         │
         │ 1. Request token
         ▼
┌──────────────────┐
│   Azure AD       │
│                  │
│  Issues token    │
└────────┬─────────┘
         │
         │ 2. Token
         ▼
┌──────────────────┐
│  Azure OpenAI    │
│                  │
│  Role Assignment:│
│  Cognitive       │
│  Services        │
│  OpenAI User     │
└──────────────────┘
```

## Data Flow

### User submits expense

```
Browser → /AddExpense (Razor Page)
         ↓
         POST to Database Service
         ↓
         EXEC dbo.CreateExpense stored procedure
         ↓
         SQL Database inserts record
         ↓
         Return expense ID
         ↓
         Redirect to /Index
```

### Manager approves expense

```
Browser → /ApproveExpenses (Razor Page)
         ↓
         Display expenses with StatusId = 2 (Submitted)
         ↓
         User clicks "Approve"
         ↓
         POST asp-page-handler="Approve"
         ↓
         EXEC dbo.ApproveExpense @expenseId, @reviewerId
         ↓
         SQL updates StatusId = 3, ReviewedBy, ReviewedAt
         ↓
         Redirect back to page
```

### AI Chat creates expense

```
Browser → /Chat (Razor Page with JavaScript)
         ↓
         User: "Add a £50 lunch expense for today"
         ↓
         POST /api/chat
         ↓
         ChatService calls Azure OpenAI
         ↓
         OpenAI decides to call function: create_expense
         ↓
         ChatService.ExecuteFunction()
         ↓
         DatabaseService.CreateExpenseAsync()
         ↓
         EXEC dbo.CreateExpense
         ↓
         Return result to OpenAI
         ↓
         OpenAI generates friendly response
         ↓
         Return to browser
```

## Security Architecture

### Entra ID Only Authentication

- ✅ No SQL passwords stored anywhere
- ✅ No connection strings with credentials
- ✅ Azure AD manages authentication
- ✅ Tokens expire automatically
- ✅ Centralized identity management

### Managed Identity Benefits

- ✅ No secrets in code or configuration
- ✅ Automatic credential rotation
- ✅ Scoped to specific Azure resources
- ✅ Auditable in Azure AD logs
- ✅ Cannot be extracted or stolen

### Network Security

- ✅ SQL Database: Azure services firewall rule only
- ✅ App Service: HTTPS only, TLS 1.2+
- ✅ App Service: FTPS disabled
- ✅ All Azure backbone traffic encrypted

## Monitoring & Diagnostics

### Application Insights Collects

- HTTP request logs (response times, status codes)
- Console logs from application
- Application logs (Info, Warning, Error)
- Custom events and metrics
- Exceptions and stack traces
- Dependencies (SQL, OpenAI calls)

### SQL Database Diagnostics

- Query performance statistics
- Blocking and deadlocks
- Failed login attempts
- Timeout events
- Automatic tuning recommendations

### Log Analytics Queries

All logs centralized in Log Analytics Workspace for:
- Cross-service correlation
- Advanced queries (KQL)
- Alerting rules
- Dashboards
- Long-term retention

## Scaling Considerations

### Current Configuration

- App Service: Standard S1 (100 total ACU, 1.75 GB RAM)
- SQL Database: Basic (5 DTU, 2 GB storage)
- Azure OpenAI: 8 capacity units

### Scaling Up

**App Service:**
- S2: 200 ACU, 3.5 GB RAM
- S3: 400 ACU, 7 GB RAM
- P1-P3: Premium tier with auto-scale

**SQL Database:**
- Standard: S0-S12 (10-3000 DTUs)
- Premium: P1-P15 (125-4000 DTUs)
- Hyperscale: Unlimited storage, read replicas

**Azure OpenAI:**
- Increase capacity: 8 → 16 → 32
- Add multiple deployments for load balancing

### High Availability

- App Service: Multiple instances with load balancer
- SQL Database: Geo-replication, failover groups
- Azure OpenAI: Multi-region deployments

## Cost Optimization

### Current Monthly Estimate (UK South)

- App Service S1: ~£55/month
- SQL Basic: ~£4/month
- Log Analytics: ~£2/month (500 MB free)
- Application Insights: Free tier
- **Total Core**: ~£61/month

### With GenAI (Sweden Central for OpenAI)

- Azure OpenAI (8 capacity): ~£1-5/month (depends on usage)
- AI Search Basic: ~£60/month
- **Total with GenAI**: ~£126/month

### Cost Savings Tips

- Use B1 App Service for dev/test (~£10/month)
- SQL Database serverless tier (pay for what you use)
- Delete resources when not in use
- Use Azure Cost Management alerts

## Deployment Patterns

### Infrastructure First

1. Deploy infrastructure (Bicep)
2. Configure database and identity
3. Deploy application code
4. Verify health endpoints

### Continuous Deployment

1. Push to GitHub
2. GitHub Actions triggers
3. OIDC authentication (no secrets)
4. Infrastructure deployed (if changed)
5. Application deployed
6. Health check runs

### Blue-Green Deployment

- Use App Service deployment slots
- Deploy to staging slot
- Warm up application
- Swap to production
- Zero downtime

## Technology Stack

- **Frontend**: Razor Pages, HTML5, CSS3, JavaScript
- **Backend**: ASP.NET Core 8.0 (Minimal APIs)
- **Database**: Azure SQL Database (T-SQL, Stored Procedures)
- **AI**: Azure OpenAI (GPT-4o), Azure AI Search
- **Authentication**: Azure Entra ID, Managed Identity
- **Monitoring**: Application Insights, Log Analytics
- **Infrastructure**: Azure Bicep (IaC)
- **CI/CD**: GitHub Actions (OIDC)
- **API**: REST with Swagger/OpenAPI documentation

## Best Practices Implemented

✅ Infrastructure as Code (Bicep)
✅ Passwordless authentication (Managed Identity)
✅ Separation of concerns (Services layer)
✅ Stored procedures (No dynamic SQL)
✅ Comprehensive error handling
✅ Structured logging
✅ Health check endpoints
✅ API documentation (Swagger)
✅ Secure configuration (Environment variables)
✅ CI/CD automation
✅ Monitoring and diagnostics
✅ TLS/HTTPS everywhere
