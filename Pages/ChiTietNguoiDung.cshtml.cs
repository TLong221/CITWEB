using CITWEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CITWEB.Pages;

public class ChiTietNguoiDungModel : PageModel
{
    public string DisplayName { get; private set; } = "Người dùng";
    public string Email { get; private set; } = "";
    public string Role { get; private set; } = AuthSessionExtensions.RoleUser;
    public bool IsAdmin => string.Equals(Role, AuthSessionExtensions.RoleAdmin, StringComparison.OrdinalIgnoreCase);
    public string Phone { get; private set; } = "";
    public int? Age { get; private set; }
    public string Bio { get; private set; } = "";

    [BindProperty]
    public string FullNameInput { get; set; } = "";

    [BindProperty]
    public string PhoneInput { get; set; } = "";

    [BindProperty]
    public int? AgeInput { get; set; }

    [BindProperty]
    public string BioInput { get; set; } = "";

    public string? FeedbackMessage { get; private set; }

    private readonly UserAccountService _accounts;

    public ChiTietNguoiDungModel(UserAccountService accounts)
    {
        _accounts = accounts;
    }

    public IActionResult OnGet()
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Dangnhap", new { ReturnUrl = "/ChiTietNguoiDung" });

        LoadProfile();
        return Page();
    }

    public IActionResult OnPostSaveProfile()
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Dangnhap", new { ReturnUrl = "/ChiTietNguoiDung" });

        var email = HttpContext.Session.GetUserEmail() ?? "";
        if (!_accounts.UpdateProfile(email, FullNameInput, PhoneInput, AgeInput, BioInput, out var updated, out var error) || updated is null)
        {
            LoadProfile();
            FeedbackMessage = error;
            return Page();
        }

        HttpContext.Session.SetUser(updated.Email, updated.FullName, updated.Role);
        LoadProfile();
        FeedbackMessage = "Đã cập nhật hồ sơ thành công.";
        return Page();
    }

    private void LoadProfile()
    {
        var email = HttpContext.Session.GetUserEmail() ?? "";
        if (_accounts.TryGetByEmail(email, out var user) && user is not null)
        {
            DisplayName = user.FullName;
            Email = user.Email;
            Role = user.Role;
            Phone = user.Phone;
            Age = user.Age;
            Bio = user.Bio;
        }
        else
        {
            DisplayName = HttpContext.Session.GetUserName() ?? "Người dùng";
            Email = email;
            Role = HttpContext.Session.GetUserRole();
        }

        FullNameInput = DisplayName;
        PhoneInput = Phone;
        AgeInput = Age;
        BioInput = Bio;
    }
}
