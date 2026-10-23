using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Abstract;

public interface IAktiviteService
{
    Task<List<AktiviteKaydi>> SonKayitlarAsync(int yasliId, int gun = 7);
    Task<AktiviteKaydi> HatirlatmaOnaylaAsync(int hatirlatmaId, AktiviteDurumu durum, string? not = null);
    Task<AktiviteKaydi> GunlukDurumBildirAsync(int yasliId, RuhHali ruhHali, string? not = null);
}
