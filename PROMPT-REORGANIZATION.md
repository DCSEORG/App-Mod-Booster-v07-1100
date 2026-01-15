# Prompt Reorganization Summary

## Overview
Reorganized 25+ prompts into 13 focused, actionable prompts to optimize the app-mod-booster agent's performance.

## Key Changes

### Before
- 25+ prompt files with overlapping concerns
- Redundant instructions scattered across files
- Required significant planning from the agent
- Mixed multiple functionalities in single prompts
- Verbose explanations requiring interpretation

### After
- 13 streamlined prompts, one per discrete functionality
- All considerations retained and consolidated
- Minimal planning required - each prompt is immediately actionable
- Clear separation of concerns
- Concise, directive instructions

## New Prompt Structure

1. **prompt-001-create-managed-identity** - Creates user-assigned managed identity
2. **prompt-002-create-app-service** - Creates App Service with identity
3. **prompt-003-create-azure-sql** - Creates Azure SQL with Entra ID auth
4. **prompt-004-import-database-schema** - Python script to import schema
5. **prompt-005-configure-database-roles** - Sets up managed identity DB permissions
6. **prompt-006-create-stored-procedures** - Creates stored procs for data access
7. **prompt-007-create-application-code** - Creates ASP.NET Razor Pages app
8. **prompt-008-create-api-endpoints** - Creates REST APIs with Swagger
9. **prompt-009-create-deployment-scripts** - Creates deploy.sh and deploy-with-chat.sh
10. **prompt-010-create-genai-resources** - Creates Azure OpenAI and Cognitive Search
11. **prompt-011-create-chat-ui** - Creates chat UI with RAG pattern
12. **prompt-012-implement-function-calling** - Implements Azure OpenAI function calling
13. **prompt-013-create-architecture-diagram** - Creates architecture diagram

## Consolidated Considerations

All critical considerations from original prompts are retained:

### Security & Authentication
- Entra ID (Azure AD) only authentication
- Managed identity for all Azure service connections
- No SQL authentication, no API keys
- XSS prevention in chat UI
- MCAPS governance policy compliance

### Technical Requirements
- .NET 8 (LTS) target framework
- Lowercase resource naming
- uniqueString() for resource uniqueness (no timestamps)
- Stable API versions (@2021-11-01, not preview)
- Cross-platform script compatibility (Mac/Linux)

### Deployment Best Practices
- 30-second waits for resource readiness
- Proper deployment order
- SQL firewall configuration (current IP + Azure services)
- Post-deployment configuration via scripts (breaks circular dependencies)
- App.zip at root level (not nested)

### Database Access Patterns
- Stored procedures only (no direct SQL in app)
- No direct table access
- APIs as single source of truth
- Managed identity authentication

### Error Handling
- Dummy data fallback on connection failures
- Detailed error messages with actionable fixes
- User-friendly error displays

### GenAI Integration
- GPT-4o in swedencentral (avoid quota issues)
- Function calling for database operations
- RAG pattern with contextual information
- Formatted list display in chat bubbles
- Optional deployment (deploy.sh vs deploy-with-chat.sh)

## Performance Improvements

1. **Reduced context switching** - Agent processes one clear task at a time
2. **Eliminated redundancy** - No duplicate instructions across prompts
3. **Minimal planning** - Each prompt is self-contained and actionable
4. **Clear dependencies** - Sequential order matches infrastructure dependencies
5. **Focused scope** - Each prompt has single responsibility

## Execution Flow

The agent follows prompt-order sequentially:
1. Infrastructure (identity, app service, SQL)
2. Database setup (schema, roles, stored procedures)
3. Application code (app, APIs)
4. Deployment automation (scripts)
5. Optional GenAI features (Azure OpenAI, chat UI, function calling)
6. Documentation (architecture diagram)

## Result

The reorganized prompts enable the app-mod-booster agent to:
- Execute faster by reducing planning overhead
- Avoid confusion from redundant instructions
- Follow a clear, logical progression
- Handle each discrete functionality independently
- Scale better if additional features are needed

All original functionality and considerations are preserved while significantly improving execution efficiency.
