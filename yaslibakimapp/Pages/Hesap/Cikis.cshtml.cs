using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace yaslibakimapp.Pages.Hesap;

// Çıkış yalnızca POST + anti-forgery token ile yapılır; başka bir sitedeki <img src="/Hesap/Cikis"> kullanıcıyı çıkaramaz.
public class CikisModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Hesap/Giris");
    }
}
