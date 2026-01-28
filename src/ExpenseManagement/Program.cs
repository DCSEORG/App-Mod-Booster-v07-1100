using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Expense Management API", 
        Version = "v1",
        Description = "API for managing expenses, categories, and users"
    });
});

// Register application services
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<DummyDataService>();
builder.Services.AddSingleton<ChatService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


var app = builder.Build();

// Test database connection on startup
var dbService = app.Services.GetRequiredService<DatabaseService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    var isConnected = await dbService.TestConnectionAsync();
    if (isConnected)
    {
        logger.LogInformation("✓ Database connection successful");
    }
    else
    {
        logger.LogWarning("⚠ Database connection failed - using dummy data");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "⚠ Database connection error - using dummy data");
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Management API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseRouting();

// Global error handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        if (exception != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "Unhandled exception");
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var stackTrace = new StackTrace(exception, true);
        var frame = stackTrace.GetFrame(0);
        var fileName = frame?.GetFileName() ?? "Unknown";
        var lineNumber = frame?.GetFileLineNumber() ?? 0;

        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
            exception?.Message ?? "An unexpected error occurred",
            $"{fileName}:{lineNumber}"
        ));
    });
});

app.MapRazorPages();

// API Endpoints

// Health check
app.MapGet("/api/health", (DatabaseService db) => new
{
    status = "healthy",
    databaseConnected = db.IsConnected,
    timestamp = DateTime.UtcNow
})
.WithName("HealthCheck")
.WithTags("System")
;

// Expenses API
app.MapGet("/api/expenses", async (DatabaseService db, DummyDataService dummy) =>
{
    try
    {
        if (!db.IsConnected)
        {
            return Results.Ok(ApiResponse<List<Expense>>.Ok(dummy.GetExpenses()));
        }
        var expenses = await db.GetExpensesAsync();
        return Results.Ok(ApiResponse<List<Expense>>.Ok(expenses));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<List<Expense>>.Fail(ex.Message));
    }
})
.WithName("GetExpenses")
.WithTags("Expenses")
;

app.MapGet("/api/expenses/status/{status}", async (string status, DatabaseService db, DummyDataService dummy) =>
{
    try
    {
        if (!db.IsConnected)
        {
            return Results.Ok(ApiResponse<List<Expense>>.Ok(dummy.GetExpensesByStatus(status)));
        }
        var expenses = await db.GetExpensesByStatusAsync(status);
        return Results.Ok(ApiResponse<List<Expense>>.Ok(expenses));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<List<Expense>>.Fail(ex.Message));
    }
})
.WithName("GetExpensesByStatus")
.WithTags("Expenses")
;

app.MapGet("/api/expenses/{id:int}", async (int id, DatabaseService db) =>
{
    try
    {
        var expense = await db.GetExpenseByIdAsync(id);
        if (expense == null)
        {
            return Results.NotFound(ApiResponse<Expense>.Fail("Expense not found"));
        }
        return Results.Ok(ApiResponse<Expense>.Ok(expense));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<Expense>.Fail(ex.Message));
    }
})
.WithName("GetExpenseById")
.WithTags("Expenses")
;

app.MapPost("/api/expenses", async (CreateExpenseRequest request, DatabaseService db) =>
{
    try
    {
        var expenseId = await db.CreateExpenseAsync(request);
        return Results.Ok(ApiResponse<int>.Ok(expenseId));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<int>.Fail(ex.Message));
    }
})
.WithName("CreateExpense")
.WithTags("Expenses")
;

app.MapPut("/api/expenses", async (UpdateExpenseRequest request, DatabaseService db) =>
{
    try
    {
        var rowsAffected = await db.UpdateExpenseAsync(request);
        if (rowsAffected == 0)
        {
            return Results.NotFound(ApiResponse<int>.Fail("Expense not found or not updated"));
        }
        return Results.Ok(ApiResponse<int>.Ok(rowsAffected));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<int>.Fail(ex.Message));
    }
})
.WithName("UpdateExpense")
.WithTags("Expenses")
;

app.MapPost("/api/expenses/{id:int}/submit", async (int id, DatabaseService db) =>
{
    try
    {
        var rowsAffected = await db.SubmitExpenseAsync(id);
        return Results.Ok(ApiResponse<int>.Ok(rowsAffected));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<int>.Fail(ex.Message));
    }
})
.WithName("SubmitExpense")
.WithTags("Expenses")
;

app.MapPost("/api/expenses/{id:int}/approve", async (int id, int reviewerId, DatabaseService db) =>
{
    try
    {
        var rowsAffected = await db.ApproveExpenseAsync(id, reviewerId);
        return Results.Ok(ApiResponse<int>.Ok(rowsAffected));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<int>.Fail(ex.Message));
    }
})
.WithName("ApproveExpense")
.WithTags("Expenses")
;

app.MapPost("/api/expenses/{id:int}/reject", async (int id, int reviewerId, DatabaseService db) =>
{
    try
    {
        var rowsAffected = await db.RejectExpenseAsync(id, reviewerId);
        return Results.Ok(ApiResponse<int>.Ok(rowsAffected));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<int>.Fail(ex.Message));
    }
})
.WithName("RejectExpense")
.WithTags("Expenses")
;

app.MapDelete("/api/expenses/{id:int}", async (int id, DatabaseService db) =>
{
    try
    {
        var rowsAffected = await db.DeleteExpenseAsync(id);
        return Results.Ok(ApiResponse<int>.Ok(rowsAffected));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<int>.Fail(ex.Message));
    }
})
.WithName("DeleteExpense")
.WithTags("Expenses")
;

// Categories API
app.MapGet("/api/categories", async (DatabaseService db, DummyDataService dummy) =>
{
    try
    {
        if (!db.IsConnected)
        {
            return Results.Ok(ApiResponse<List<ExpenseCategory>>.Ok(dummy.GetCategories()));
        }
        var categories = await db.GetCategoriesAsync();
        return Results.Ok(ApiResponse<List<ExpenseCategory>>.Ok(categories));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<List<ExpenseCategory>>.Fail(ex.Message));
    }
})
.WithName("GetCategories")
.WithTags("Categories")
;

// Users API
app.MapGet("/api/users", async (DatabaseService db, DummyDataService dummy) =>
{
    try
    {
        if (!db.IsConnected)
        {
            return Results.Ok(ApiResponse<List<User>>.Ok(dummy.GetUsers()));
        }
        var users = await db.GetUsersAsync();
        return Results.Ok(ApiResponse<List<User>>.Ok(users));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<List<User>>.Fail(ex.Message));
    }
})
.WithName("GetUsers")
.WithTags("Users")
;

app.MapGet("/api/users/{id:int}", async (int id, DatabaseService db) =>
{
    try
    {
        var user = await db.GetUserByIdAsync(id);
        if (user == null)
        {
            return Results.NotFound(ApiResponse<User>.Fail("User not found"));
        }
        return Results.Ok(ApiResponse<User>.Ok(user));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<User>.Fail(ex.Message));
    }
})
.WithName("GetUserById")
.WithTags("Users")
;

// Statuses API
app.MapGet("/api/statuses", async (DatabaseService db, DummyDataService dummy) =>
{
    try
    {
        if (!db.IsConnected)
        {
            return Results.Ok(ApiResponse<List<ExpenseStatus>>.Ok(dummy.GetStatuses()));
        }
        var statuses = await db.GetStatusesAsync();
        return Results.Ok(ApiResponse<List<ExpenseStatus>>.Ok(statuses));
    }
    catch (Exception ex)
    {
        return Results.Ok(ApiResponse<List<ExpenseStatus>>.Fail(ex.Message));
    }
})
.WithName("GetStatuses")
.WithTags("Statuses")
;

// Chat API
app.MapPost("/api/chat", async (ChatRequest request, ChatService chat) =>
{
    try
    {
        var response = await chat.SendMessageAsync(request.Message, request.History);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Ok(new ChatResponse
        {
            Success = false,
            Error = ex.Message,
            Message = "An error occurred while processing your request."
        });
    }
})
.WithName("Chat")
.WithTags("Chat")
;

app.Run();
