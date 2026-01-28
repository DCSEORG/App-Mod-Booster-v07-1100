using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;
using ChatMessageModel = ExpenseManagement.Models.ChatMessage;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly DatabaseService _databaseService;
    private AzureOpenAIClient? _openAIClient;
    private string? _modelDeploymentName;
    private bool _isConfigured = false;

    public ChatService(
        IConfiguration configuration,
        ILogger<ChatService> logger,
        DatabaseService databaseService)
    {
        _configuration = configuration;
        _logger = logger;
        _databaseService = databaseService;
        
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var endpoint = _configuration["OpenAI:Endpoint"];
            _modelDeploymentName = _configuration["OpenAI:DeploymentName"];
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(_modelDeploymentName))
            {
                _logger.LogWarning("OpenAI configuration not found. Chat functionality will be disabled.");
                return;
            }

            Azure.Core.TokenCredential credential;
            
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            _openAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
            _isConfigured = true;
            
            _logger.LogInformation("Chat service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Chat service");
            _isConfigured = false;
        }
    }

    public bool IsConfigured => _isConfigured;

    public async Task<ChatResponse> SendMessageAsync(string userMessage, List<ChatMessageModel>? history = null)
    {
        if (!_isConfigured || _openAIClient == null || string.IsNullOrEmpty(_modelDeploymentName))
        {
            return new ChatResponse
            {
                Success = false,
                Error = "Chat service is not configured. Please deploy GenAI resources with the -DeployGenAI switch.",
                Message = "Chat functionality is not available. The administrator needs to deploy Azure OpenAI resources."
            };
        }

        try
        {
            var chatClient = _openAIClient.GetChatClient(_modelDeploymentName);
            
            // Build conversation history
            var messages = new List<ChatMessageModel>();
            
            // System message with instructions
            messages.Add(new ChatMessageModel
            {
                Role = "system",
                Content = @"You are a helpful assistant for an Expense Management System. You can help users:
- View expenses (all, by status, by user)
- Create new expenses
- Submit expenses for approval
- Approve or reject expenses (for managers)
- Get summaries and reports

When creating expenses, amounts should be in GBP (British Pounds) and you need:
- UserId (usually 1 for Alice, 2 for Bob)
- CategoryId (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)
- Amount (as a decimal, e.g., 25.40)
- ExpenseDate (YYYY-MM-DD format)
- Description (optional)

Always be helpful and provide clear information. Format lists and numbers nicely."
            });

            // Add conversation history
            if (history != null && history.Any())
            {
                messages.AddRange(history);
            }

            // Add current user message
            messages.Add(new ChatMessageModel { Role = "user", Content = userMessage });

            // Define function tools
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_expenses",
                    "Retrieves all expenses from the database",
                    BinaryData.FromString("{\"type\":\"object\",\"properties\":{},\"required\":[]}")
                ),
                ChatTool.CreateFunctionTool(
                    "get_expenses_by_status",
                    "Retrieves expenses filtered by status",
                    BinaryData.FromString(@"{
                        ""type"":""object"",
                        ""properties"":{
                            ""status"":{""type"":""string"",""enum"":[""Draft"",""Submitted"",""Approved"",""Rejected""],""description"":""The status to filter by""}
                        },
                        ""required"":[""status""]
                    }")
                ),
                ChatTool.CreateFunctionTool(
                    "create_expense",
                    "Creates a new expense",
                    BinaryData.FromString(@"{
                        ""type"":""object"",
                        ""properties"":{
                            ""userId"":{""type"":""integer"",""description"":""The ID of the user creating the expense""},
                            ""categoryId"":{""type"":""integer"",""description"":""Category ID (1=Travel,2=Meals,3=Supplies,4=Accommodation,5=Other)""},
                            ""amount"":{""type"":""number"",""description"":""Amount in GBP (e.g., 25.40)""},
                            ""expenseDate"":{""type"":""string"",""description"":""Date in YYYY-MM-DD format""},
                            ""description"":{""type"":""string"",""description"":""Description of the expense""}
                        },
                        ""required"":[""userId"",""categoryId"",""amount"",""expenseDate""]
                    }")
                ),
                ChatTool.CreateFunctionTool(
                    "submit_expense",
                    "Submits an expense for approval",
                    BinaryData.FromString(@"{
                        ""type"":""object"",
                        ""properties"":{
                            ""expenseId"":{""type"":""integer"",""description"":""The ID of the expense to submit""}
                        },
                        ""required"":[""expenseId""]
                    }")
                ),
                ChatTool.CreateFunctionTool(
                    "approve_expense",
                    "Approves an expense (manager only)",
                    BinaryData.FromString(@"{
                        ""type"":""object"",
                        ""properties"":{
                            ""expenseId"":{""type"":""integer"",""description"":""The ID of the expense to approve""},
                            ""reviewerId"":{""type"":""integer"",""description"":""The ID of the manager approving (usually 2 for Bob)""}
                        },
                        ""required"":[""expenseId"",""reviewerId""]
                    }")
                ),
                ChatTool.CreateFunctionTool(
                    "get_categories",
                    "Retrieves all expense categories",
                    BinaryData.FromString("{\"type\":\"object\",\"properties\":{},\"required\":[]}")
                )
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Convert messages to OpenAI format
            var openAIMessages = messages.Select(m => 
                m.Role == "system" ? (OpenAI.Chat.ChatMessage)new SystemChatMessage(m.Content) :
                m.Role == "assistant" ? new AssistantChatMessage(m.Content) :
                new UserChatMessage(m.Content)
            ).ToList();

            var response = await chatClient.CompleteChatAsync(openAIMessages, options);

            // Handle function calls
            while (response.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                foreach (var toolCall in response.Value.ToolCalls)
                {
                    if (toolCall.Kind == ChatToolCallKind.Function)
                    {
                        var functionCall = (ChatToolCall)toolCall;
                        var functionResult = await ExecuteFunctionAsync(functionCall.FunctionName, functionCall.FunctionArguments);
                        
                        // Add tool result to conversation
                        openAIMessages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                    }
                }

                // Get next response
                response = await chatClient.CompleteChatAsync(openAIMessages, options);
            }

            var assistantMessage = response.Value.Content[0].Text;

            return new ChatResponse
            {
                Success = true,
                Message = assistantMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat service");
            return new ChatResponse
            {
                Success = false,
                Error = ex.Message,
                Message = "Sorry, I encountered an error processing your request. Please try again."
            };
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, BinaryData argumentsData)
    {
        try
        {
            _logger.LogInformation("Executing function: {FunctionName}", functionName);
            
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsData.ToString());

            return functionName switch
            {
                "get_expenses" => await GetExpensesFunction(),
                "get_expenses_by_status" => await GetExpensesByStatusFunction(arguments!["status"].GetString()!),
                "create_expense" => await CreateExpenseFunction(arguments!),
                "submit_expense" => await SubmitExpenseFunction(arguments!["expenseId"].GetInt32()),
                "approve_expense" => await ApproveExpenseFunction(
                    arguments!["expenseId"].GetInt32(),
                    arguments["reviewerId"].GetInt32()
                ),
                "get_categories" => await GetCategoriesFunction(),
                _ => JsonSerializer.Serialize(new { error = "Unknown function" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> GetExpensesFunction()
    {
        var expenses = await _databaseService.GetExpensesAsync();
        return JsonSerializer.Serialize(expenses);
    }

    private async Task<string> GetExpensesByStatusFunction(string status)
    {
        var expenses = await _databaseService.GetExpensesByStatusAsync(status);
        return JsonSerializer.Serialize(expenses);
    }

    private async Task<string> CreateExpenseFunction(Dictionary<string, JsonElement> args)
    {
        var request = new CreateExpenseRequest
        {
            UserId = args["userId"].GetInt32(),
            CategoryId = args["categoryId"].GetInt32(),
            Amount = args["amount"].GetDecimal(),
            ExpenseDate = DateTime.Parse(args["expenseDate"].GetString()!),
            Description = args.ContainsKey("description") ? args["description"].GetString() : null
        };

        var expenseId = await _databaseService.CreateExpenseAsync(request);
        return JsonSerializer.Serialize(new { expenseId, message = "Expense created successfully" });
    }

    private async Task<string> SubmitExpenseFunction(int expenseId)
    {
        var result = await _databaseService.SubmitExpenseAsync(expenseId);
        return JsonSerializer.Serialize(new { success = result > 0, message = result > 0 ? "Expense submitted for approval" : "Failed to submit expense" });
    }

    private async Task<string> ApproveExpenseFunction(int expenseId, int reviewerId)
    {
        var result = await _databaseService.ApproveExpenseAsync(expenseId, reviewerId);
        return JsonSerializer.Serialize(new { success = result > 0, message = result > 0 ? "Expense approved" : "Failed to approve expense" });
    }

    private async Task<string> GetCategoriesFunction()
    {
        var categories = await _databaseService.GetCategoriesAsync();
        return JsonSerializer.Serialize(categories);
    }
}
