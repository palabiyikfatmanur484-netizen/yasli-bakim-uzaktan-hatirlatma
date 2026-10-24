using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Entities.Enums;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages.Hesap;

[AllowAnonymous]
public class GirisModel : PageModel
{
    private readonly IKimlikService _kimlikService;
    private readonly ILogger<GirisModel> _logger;

    public GirisModel(IKimlikService kimlikService, ILogger<GirisModel> logger)
    {
        _kimlikService = kimlikService;
        _logger = logger;
    }

    [BindProperty, Required(ErrorMessage = "E-posta zorunlu."), EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string Eposta { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Parola zorunlu."), DataType(DataType.Password)]
    public string Parola { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Hata { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var sonuc = await _kimlikService.GirisAsync(Eposta, Parola);
        if (!sonuc.Basarili)
        {
            // Parola asla loglanmaz.
            _logger.LogWarning("Başarısız giriş denemesi: {Eposta}, IP {Ip}", Eposta, HttpContext.Connection.RemoteIpAddress);
            Hata = sonuc.Hata;
            return Page();
        }

        var kullanici = sonuc.Kullanici!;
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, KullaniciClaimleri.Olustur(kullanici));
        _logger.LogInformation("Giriş yapıldı: {Eposta} ({Rol})", kullanici.Eposta, kullanici.Rol);

        // Açık yönlendirme (open redirect) koruması: yalnızca kendi sitemizdeki adreslere dönülür.
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return LocalRedirect(ReturnUrl);

        return kullanici.Rol == KullaniciRolu.Yasli ? RedirectToPage("/Panelim") : RedirectToPage("/Index");
    }
}
