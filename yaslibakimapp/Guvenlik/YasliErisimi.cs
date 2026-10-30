using System.Security.Claims;
using YasliBakim.Business.Abstract;

namespace yaslibakimapp.Guvenlik;

/// <summary>
/// Kayıt sahipliği kontrolü: bir yaşlının verisini görme/değiştirme yetkisi.
/// Yönetici herkese, bakıcı yalnızca kendisine atanmış yaşlıya, yaşlı yalnızca kendisine erişir.
/// </summary>
public static class YasliErisimi
{
    public static async Task<bool> YasliyaErisebilirMiAsync(this ClaimsPrincipal user, int yasliId, IBakiciService bakiciService)
    {
        if (user.IsInRole(Roller.Yonetici))
            return true;
        if (user.BakiciId() is { } bakiciId)
            return await bakiciService.YasliyaAtanmisMiAsync(bakiciId, yasliId);
        return user.YasliId() == yasliId;
    }
}
