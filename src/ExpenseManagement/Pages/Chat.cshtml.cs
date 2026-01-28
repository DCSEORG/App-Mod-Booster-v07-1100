using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    private readonly ChatService _chatService;

    public ChatModel(ChatService chatService)
    {
        _chatService = chatService;
    }

    public bool IsChatConfigured { get; set; }

    public void OnGet()
    {
        IsChatConfigured = _chatService.IsConfigured;
    }
}
