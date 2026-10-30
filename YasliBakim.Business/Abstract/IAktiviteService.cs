using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Abstract;

// Hatırlatma onayları IHatirlatmaTakipService içindedir; burada geçmiş ve günlük durum bildirimi kalır.
public interface IAktiviteService
{
    Task<List<AktiviteKaydi>> SonKayitlarAsync(int yasliId, int gun = 7);
    Task<AktiviteKaydi> GunlukDurumBildirAsync(int yasliId, RuhHali ruhHali, string? not = null);
}
