using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IHatirlatmaRepository : IGenericRepository<Hatirlatma>
{
    // Verilen tarihte tarih aralığı geçerli olan aktif hatırlatmalar, saate göre sıralı.
    // Tekrar tipine (haftalık/aylık) göre o gün gerçekleşip gerçekleşmediğine iş katmanı karar verir.
    Task<List<Hatirlatma>> GetGecerliHatirlatmalarAsync(int yasliId, DateOnly tarih);

    // Tüm yaşlıların, [baslangic, bitis] aralığıyla kesişen aktif hatırlatmaları (kaçırma kontrolü için).
    Task<List<Hatirlatma>> GetAktifHatirlatmalarAsync(DateOnly baslangic, DateOnly bitis);

    // Aynı yaşlıya aynı saatte aynı başlıkla aktif bir hatırlatma var mı? (tekrar kayıt engeli)
    Task<bool> AyniHatirlatmaVarMiAsync(int yasliId, string baslik, TimeOnly saat, int haricId = 0);
}
