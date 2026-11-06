using CITWEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CITWEB.Pages;

public class NhatkyModel : PageModel
{
    public IActionResult OnGet()
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Dangnhap", new { ReturnUrl = "/Nhatky" });

        return Page();
    }
}
