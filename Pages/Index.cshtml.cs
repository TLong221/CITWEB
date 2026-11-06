using CITWEB.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CITWEB.Pages;

public class IndexModel : PageModel
{
    public string DisplayName { get; private set; } = "bạn";
    public bool IsAdmin { get; private set; }

    public void OnGet()
    {
        DisplayName = HttpContext.Session.GetUserName() ?? "bạn";
        IsAdmin = HttpContext.Session.IsAdmin();
    }
}
