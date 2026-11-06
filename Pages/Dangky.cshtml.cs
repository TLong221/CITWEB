using CITWEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CITWEB.Pages;

public class DangkyModel : PageModel
{
    private readonly UserAccountService _accounts;

    public DangkyModel(UserAccountService accounts)
    {
        _accounts = accounts;
    }

    [BindProperty]
    public string FullName { get; set; } = "";

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty]
    public bool AcceptTerms { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Index");

        return Page();
    }

    public IActionResult OnPost()
    {
        if (!AcceptTerms)
        {
            ErrorMessage = "Bạn cần đồng ý điều khoản dịch vụ.";
            return Page();
        }

        if (!_accounts.TryRegister(Email, Password, FullName, out var error, out var createdUser) || createdUser is null)
        {
            ErrorMessage = error;
            return Page();
        }

        HttpContext.Session.SetUser(createdUser.Email, createdUser.FullName, createdUser.Role);
        return RedirectToPage("/Index");
    }
}
