using CITWEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CITWEB.Pages;

public class AdminModel : PageModel
{
    private readonly UserAccountService _accounts;

    public AdminModel(UserAccountService accounts)
    {
        _accounts = accounts;
    }

    public string AdminName { get; private set; } = "Admin";
    public List<UserVm> Users { get; private set; } = [];
    public string? FeedbackMessage { get; private set; }

    [BindProperty]
    public string? EditingId { get; set; }

    [BindProperty]
    public string FullName { get; set; } = "";

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Role { get; set; } = AuthSessionExtensions.RoleUser;

    [BindProperty]
    public bool IsActive { get; set; } = true;

    [BindProperty]
    public string Password { get; set; } = "";

    public IActionResult OnGet()
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Dangnhap", new { ReturnUrl = "/Admin" });

        if (!HttpContext.Session.IsAdmin())
            return RedirectToPage("/Index");

        AdminName = HttpContext.Session.GetUserName() ?? "Admin";
        LoadUsers();
        return Page();
    }

    public IActionResult OnPostSaveUser()
    {
        var gate = EnsureAdmin();
        if (gate is not null) return gate;

        if (string.IsNullOrWhiteSpace(EditingId))
        {
            if (!_accounts.AdminCreateUser(FullName, Email, Password, Role, IsActive, out var createError))
            {
                FeedbackMessage = createError;
                LoadUsers();
                return Page();
            }
            FeedbackMessage = "Đã tạo người dùng mới.";
        }
        else
        {
            if (!_accounts.AdminUpdateUser(EditingId, FullName, Role, IsActive, Password, out var updateError))
            {
                FeedbackMessage = updateError;
                LoadUsers();
                return Page();
            }
            FeedbackMessage = "Đã cập nhật người dùng.";
        }

        ResetForm();
        LoadUsers();
        return Page();
    }

    public IActionResult OnPostEditUser(string id)
    {
        var gate = EnsureAdmin();
        if (gate is not null) return gate;

        LoadUsers();
        var target = Users.FirstOrDefault(x => x.Id == id);
        if (target is null)
        {
            FeedbackMessage = "Không tìm thấy người dùng.";
            return Page();
        }

        EditingId = target.Id;
        FullName = target.FullName;
        Email = target.Email;
        Role = target.Role;
        IsActive = target.IsActive;
        Password = "";
        return Page();
    }

    public IActionResult OnPostDeleteUser(string id)
    {
        var gate = EnsureAdmin();
        if (gate is not null) return gate;

        if (!_accounts.AdminDeleteUser(id, out var error))
        {
            FeedbackMessage = error;
            LoadUsers();
            return Page();
        }

        FeedbackMessage = "Đã xóa người dùng.";
        ResetForm();
        LoadUsers();
        return Page();
    }

    public IActionResult OnPostCancelEdit()
    {
        var gate = EnsureAdmin();
        if (gate is not null) return gate;

        ResetForm();
        LoadUsers();
        return Page();
    }

    private IActionResult? EnsureAdmin()
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Dangnhap", new { ReturnUrl = "/Admin" });
        if (!HttpContext.Session.IsAdmin())
            return RedirectToPage("/Index");

        AdminName = HttpContext.Session.GetUserName() ?? "Admin";
        return null;
    }

    private void ResetForm()
    {
        EditingId = "";
        FullName = "";
        Email = "";
        Role = AuthSessionExtensions.RoleUser;
        IsActive = true;
        Password = "";
    }

    private void LoadUsers()
    {
        Users = _accounts.GetAllUsers()
            .Select(u => new UserVm
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAtText = u.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm"),
                LastLoginText = u.LastLoginAt.HasValue
                    ? u.LastLoginAt.Value.LocalDateTime.ToString("dd/MM/yyyy HH:mm")
                    : "Chưa đăng nhập",
            })
            .ToList();
    }

    public sealed class UserVm
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public string CreatedAtText { get; set; } = "";
        public string LastLoginText { get; set; } = "";
    }
}
