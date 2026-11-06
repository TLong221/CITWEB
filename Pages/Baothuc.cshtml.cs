using CITWEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CITWEB.Pages;

public class BaothucModel : PageModel
{
    public IActionResult OnGet()
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Dangnhap", new { ReturnUrl = "/Baothuc" });

        return Page();
    }
}
