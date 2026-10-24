using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages.Hesap;

/// <summary>
/// Herkese açık kayıt: yalnızca bakıcı / aile yakını hesabı açılır. Formda rol alanı yoktur;
/// biri isteğe "Rol=Yonetici" eklese bile bağlanacak bir özellik olmadığı için yok sayılır.
/// </summary>
[AllowAnonymous]
public class KayitModel : PageModel
{
    private readonly IKimlikService _kimlikService;

    public KayitModel(IKimlikService kimlikService)
    {
        _kimlikService = kimlikService;
    }

    [BindProperty, Required(ErrorMessage = "Ad zorunlu."), StringLength(50)]
    public string Ad { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Soyad zorunlu."), StringLength(50)]
    public string Soyad { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "E-posta zorunlu."), EmailAddress(ErrorMessage = "Geçerli bir e-posta girin."), StringLength(100)]
    public string Eposta { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Telefon zorunlu."), StringLength(20)]
    public string Telefon { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Parola zorunlu."), DataType(DataType.Password)]
    public string Parola { get; set; } = "";

    [BindProperty, DataType(DataType.Password), Compare(nameof(Parola), ErrorMessage = "Parolalar eşleşmiyor.")]
    public string ParolaTekrar { get; set; } = "";

    public string? Hata { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var kullanici = await _kimlikService.BakiciKayitAsync(Ad, Soyad, Eposta, Telefon, Parola);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, KullaniciClaimleri.Olustur(kullanici));
            return RedirectToPage("/Index");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Hata = ex.Message;
            return Page();
        }
    }
}
