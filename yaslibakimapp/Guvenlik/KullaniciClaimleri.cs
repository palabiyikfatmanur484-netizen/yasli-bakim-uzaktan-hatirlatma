using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using YasliBakim.Entities.Concrete;

namespace yaslibakimapp.Guvenlik;

/// <summary>
/// Oturum çerezine yazılan bilgiler (claim). Çerez sunucunun anahtarıyla şifrelenip imzalandığı için
/// kullanıcı içeriğini okuyamaz veya değiştiremez (ör. rolünü "Yonetici" yapamaz).
/// </summary>
public static class KullaniciClaimleri
{
    public const string BakiciIdTipi = "bakici_id";
    public const string YasliIdTipi = "yasli_id";

    public static ClaimsPrincipal Olustur(Kullanici kullanici)
    {
        var claimler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new(ClaimTypes.Name, kullanici.Eposta),
            new(ClaimTypes.Role, kullanici.Rol.ToString()),
        };
        if (kullanici.BakiciId is not null)
            claimler.Add(new Claim(BakiciIdTipi, kullanici.BakiciId.Value.ToString()));
        if (kullanici.YasliId is not null)
            claimler.Add(new Claim(YasliIdTipi, kullanici.YasliId.Value.ToString()));

        var kimlik = new ClaimsIdentity(claimler, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(kimlik);
    }

    public static int KullaniciId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static int? BakiciId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(BakiciIdTipi), out var id) ? id : null;

    public static int? YasliId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(YasliIdTipi), out var id) ? id : null;
}
