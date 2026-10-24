using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using YasliBakim.DataAccess.Abstract;

namespace yaslibakimapp.Guvenlik;

/// <summary>
/// Her istekte çerezdeki kullanıcının hâlâ var ve aktif olduğunu kontrol eder.
/// Hesap silinir veya devre dışı bırakılırsa, çerezin süresi dolmamış olsa bile oturum hemen sona erer.
/// </summary>
public static class OturumDogrulayici
{
    public static async Task DogrulaAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal is null)
            return;

        var repository = context.HttpContext.RequestServices.GetRequiredService<IKullaniciRepository>();
        var kullanici = await repository.GetByIdAsync(context.Principal.KullaniciId());

        // Rol değiştirilmişse eski roldeki çerez de geçersiz sayılır.
        if (kullanici is null || !kullanici.AktifMi || !context.Principal.IsInRole(kullanici.Rol.ToString()))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
