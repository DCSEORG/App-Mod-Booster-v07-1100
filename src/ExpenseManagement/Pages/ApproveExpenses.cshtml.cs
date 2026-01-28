using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly DummyDataService _dummy;
    private readonly ILogger<ApproveExpensesModel> _logger;

    public ApproveExpensesModel(DatabaseService db, DummyDataService dummy, ILogger<ApproveExpensesModel> logger)
    {
        _db = db;
        _dummy = dummy;
        _logger = logger;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public bool UsingDummyData { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (!_db.IsConnected)
            {
                UsingDummyData = true;
                PendingExpenses = _dummy.GetExpensesByStatus("Submitted");
            }
            else
            {
                PendingExpenses = await _db.GetExpensesByStatusAsync("Submitted");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending expenses");
            UsingDummyData = true;
            PendingExpenses = _dummy.GetExpensesByStatus("Submitted");
            
            TempData["ErrorMessage"] = $"Error loading data: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId)
    {
        try
        {
            if (!_db.IsConnected)
            {
                TempData["ErrorMessage"] = "Cannot approve expense - database not connected";
                return RedirectToPage();
            }

            // Using reviewer ID 2 (Bob Manager)
            await _db.ApproveExpenseAsync(expenseId, 2);
            TempData["SuccessMessage"] = $"Expense {expenseId} approved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            TempData["ErrorMessage"] = $"Failed to approve expense: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId)
    {
        try
        {
            if (!_db.IsConnected)
            {
                TempData["ErrorMessage"] = "Cannot reject expense - database not connected";
                return RedirectToPage();
            }

            await _db.RejectExpenseAsync(expenseId, 2);
            TempData["SuccessMessage"] = $"Expense {expenseId} rejected";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            TempData["ErrorMessage"] = $"Failed to reject expense: {ex.Message}";
        }

        return RedirectToPage();
    }
}
