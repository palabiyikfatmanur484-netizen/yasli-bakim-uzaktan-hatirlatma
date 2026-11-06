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

    /// <summary>
    /// Sayfanın hangi yaşlı için çalışacağı. Yaşlı için her zaman oturumdaki kendi Id'si döner
    /// (adres çubuğuna başka bir yasliId yazılsa bile yok sayılır); diğer roller için verilen yasliId
    /// erişim kontrolünden geçerse döner. null = erişim yok.
    /// </summary>
    public static async Task<int?> HedefYasliAsync(this ClaimsPrincipal user, int? yasliId, IBakiciService bakiciService)
    {
        if (user.YasliId() is { } kendisi)
            return kendisi;
        if (yasliId is null)
            return null;
        return await user.YasliyaErisebilirMiAsync(yasliId.Value, bakiciService) ? yasliId : null;
    }
}
