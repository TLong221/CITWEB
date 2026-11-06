using CITWEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CITWEB.Pages;

public class DangxuatModel : PageModel
{
    public IActionResult OnGet()
    {
        HttpContext.Session.ClearUser();
        return RedirectToPage("/Dangnhap");
    }
}
