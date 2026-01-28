using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;
    private bool _isConnected = false;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            _isConnected = true;
            _logger.LogInformation("Database connection successful");
            return true;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError(ex, "Database connection failed");
            return false;
        }
    }

    public bool IsConnected => _isConnected;

    // Expense Operations
    public async Task<List<Expense>> GetExpensesAsync()
    {
        var expenses = new List<Expense>();
        
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.GetExpenses", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            expenses.Add(MapExpenseFromReader(reader));
        }
        
        return expenses;
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(string statusName)
    {
        var expenses = new List<Expense>();
        
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.GetExpensesByStatus", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@StatusName", statusName);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            expenses.Add(MapExpenseFromReader(reader));
        }
        
        return expenses;
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.GetExpenseById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@ExpenseId", expenseId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return MapExpenseFromReader(reader);
        }
        
        return null;
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.CreateExpense", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        command.Parameters.AddWithValue("@UserId", request.UserId);
        command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
        command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
        command.Parameters.AddWithValue("@Currency", request.Currency);
        command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
        command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.UpdateExpense", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
        command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
        command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
        command.Parameters.AddWithValue("@Currency", request.Currency);
        command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
        command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return reader.GetInt32(0);
        }
        
        return 0;
    }

    public async Task<int> SubmitExpenseAsync(int expenseId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.SubmitExpense", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@ExpenseId", expenseId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return reader.GetInt32(0);
        }
        
        return 0;
    }

    public async Task<int> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.ApproveExpense", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@ExpenseId", expenseId);
        command.Parameters.AddWithValue("@ReviewerId", reviewerId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return reader.GetInt32(0);
        }
        
        return 0;
    }

    public async Task<int> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.RejectExpense", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@ExpenseId", expenseId);
        command.Parameters.AddWithValue("@ReviewerId", reviewerId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return reader.GetInt32(0);
        }
        
        return 0;
    }

    public async Task<int> DeleteExpenseAsync(int expenseId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.DeleteExpense", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@ExpenseId", expenseId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return reader.GetInt32(0);
        }
        
        return 0;
    }

    // Category Operations
    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        var categories = new List<ExpenseCategory>();
        
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.GetCategories", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            categories.Add(new ExpenseCategory
            {
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            });
        }
        
        return categories;
    }

    // User Operations
    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();
        
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.GetUsers", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            users.Add(MapUserFromReader(reader));
        }
        
        return users;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.GetUserById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@UserId", userId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return MapUserFromReader(reader);
        }
        
        return null;
    }

    // Status Operations
    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.GetStatuses", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            statuses.Add(new ExpenseStatus
            {
                StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
            });
        }
        
        return statuses;
    }

    // Helper methods
    private Expense MapExpenseFromReader(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            AmountDecimal = reader.GetDecimal(reader.GetOrdinal("AmountDecimal")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private User MapUserFromReader(SqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
            ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
