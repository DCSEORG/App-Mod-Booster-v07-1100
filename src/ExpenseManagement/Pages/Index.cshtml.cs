using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly DummyDataService _dummy;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DatabaseService db, DummyDataService dummy, ILogger<IndexModel> logger)
    {
        _db = db;
        _dummy = dummy;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public bool UsingDummyData { get; set; }
    public string? FilterStatus { get; set; }

    public async Task OnGetAsync(string? status = null)
    {
        try
        {
            FilterStatus = status;
            
            if (!_db.IsConnected)
            {
                UsingDummyData = true;
                Expenses = string.IsNullOrEmpty(status) 
                    ? _dummy.GetExpenses() 
                    : _dummy.GetExpensesByStatus(status);
                
                TempData["ErrorMessage"] = "Database connection failed - showing sample data";
                TempData["ErrorHelp"] = "Check that AZURE_CLIENT_ID and ConnectionStrings__DefaultConnection are configured";
            }
            else
            {
                Expenses = string.IsNullOrEmpty(status)
                    ? await _db.GetExpensesAsync()
                    : await _db.GetExpensesByStatusAsync(status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            UsingDummyData = true;
            Expenses = _dummy.GetExpenses();
            
            TempData["ErrorMessage"] = $"Error loading data: {ex.Message}";
            TempData["ErrorLocation"] = $"{ex.TargetSite?.DeclaringType?.Name}.{ex.TargetSite?.Name}";
        }
    }
}
