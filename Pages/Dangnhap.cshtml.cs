using CITWEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CITWEB.Pages;

public class DangnhapModel : PageModel
{
    private readonly UserAccountService _accounts;

    public DangnhapModel(UserAccountService accounts)
    {
        _accounts = accounts;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Index");

        return Page();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập email và mật khẩu.";
            return Page();
        }

        if (_accounts.TryLogin(Email, Password, out var user) && user is not null)
        {
            HttpContext.Session.SetUser(Email, user.FullName, user.Role);

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            if (string.Equals(user.Role, AuthSessionExtensions.RoleAdmin, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage("/Admin");

            return RedirectToPage("/Index");
        }

        ErrorMessage = "Email hoặc mật khẩu không đúng.";
        return Page();
    }
}
