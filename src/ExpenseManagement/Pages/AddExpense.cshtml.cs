using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly DummyDataService _dummy;
    private readonly ILogger<AddExpenseModel> _logger;

    public AddExpenseModel(DatabaseService db, DummyDataService dummy, ILogger<AddExpenseModel> logger)
    {
        _db = db;
        _dummy = dummy;
        _logger = logger;
    }

    [BindProperty]
    public CreateExpenseRequest NewExpense { get; set; } = new();

    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<User> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            if (!_db.IsConnected)
            {
                Categories = _dummy.GetCategories();
                Users = _dummy.GetUsers();
            }
            else
            {
                Categories = await _db.GetCategoriesAsync();
                Users = await _db.GetUsersAsync();
            }

            // Set defaults
            NewExpense.UserId = Users.FirstOrDefault()?.UserId ?? 1;
            NewExpense.CategoryId = Categories.FirstOrDefault()?.CategoryId ?? 1;
            NewExpense.ExpenseDate = DateTime.Today;
            NewExpense.Currency = "GBP";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading form data");
            Categories = _dummy.GetCategories();
            Users = _dummy.GetUsers();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!_db.IsConnected)
            {
                TempData["ErrorMessage"] = "Cannot create expense - database not connected";
                return RedirectToPage("/Index");
            }

            var expenseId = await _db.CreateExpenseAsync(NewExpense);
            TempData["SuccessMessage"] = $"Expense created successfully (ID: {expenseId})";
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            TempData["ErrorMessage"] = $"Failed to create expense: {ex.Message}";
            
            // Reload form data
            await OnGetAsync();
            return Page();
        }
    }
}
