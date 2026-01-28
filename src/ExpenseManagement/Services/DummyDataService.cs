using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class DummyDataService
{
    private readonly List<Expense> _dummyExpenses;
    private readonly List<ExpenseCategory> _dummyCategories;
    private readonly List<User> _dummyUsers;
    private readonly List<ExpenseStatus> _dummyStatuses;

    public DummyDataService()
    {
        // Initialize dummy data
        _dummyStatuses = new List<ExpenseStatus>
        {
            new() { StatusId = 1, StatusName = "Draft" },
            new() { StatusId = 2, StatusName = "Submitted" },
            new() { StatusId = 3, StatusName = "Approved" },
            new() { StatusId = 4, StatusName = "Rejected" }
        };

        _dummyCategories = new List<ExpenseCategory>
        {
            new() { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new() { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new() { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new() { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new() { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };

        _dummyUsers = new List<User>
        {
            new() {
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                RoleId = 1,
                RoleName = "Employee",
                ManagerId = 2,
                ManagerName = "Bob Manager",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new() {
                UserId = 2,
                UserName = "Bob Manager",
                Email = "bob.manager@example.co.uk",
                RoleId = 2,
                RoleName = "Manager",
                ManagerId = null,
                ManagerName = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1)
            }
        };

        _dummyExpenses = new List<Expense>
        {
            new() {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 2540,
                AmountDecimal = 25.40m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-10),
                Description = "Taxi from airport to client site",
                SubmittedAt = DateTime.Now.AddDays(-9),
                CreatedAt = DateTime.Now.AddDays(-10)
            },
            new() {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 1425,
                AmountDecimal = 14.25m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-30),
                Description = "Client lunch meeting",
                SubmittedAt = DateTime.Now.AddDays(-29),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-28),
                CreatedAt = DateTime.Now.AddDays(-30)
            },
            new() {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 1,
                StatusName = "Draft",
                AmountMinor = 799,
                AmountDecimal = 7.99m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-2),
                Description = "Office stationery",
                CreatedAt = DateTime.Now.AddDays(-2)
            },
            new() {
                ExpenseId = 4,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 4,
                CategoryName = "Accommodation",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 12300,
                AmountDecimal = 123.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-45),
                Description = "Hotel during client visit",
                SubmittedAt = DateTime.Now.AddDays(-44),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-43),
                CreatedAt = DateTime.Now.AddDays(-45)
            }
        };
    }

    public List<Expense> GetExpenses() => _dummyExpenses;

    public List<Expense> GetExpensesByStatus(string statusName)
        => _dummyExpenses.Where(e => e.StatusName.Equals(statusName, StringComparison.OrdinalIgnoreCase)).ToList();

    public List<ExpenseCategory> GetCategories() => _dummyCategories;

    public List<User> GetUsers() => _dummyUsers;

    public List<ExpenseStatus> GetStatuses() => _dummyStatuses;
}
