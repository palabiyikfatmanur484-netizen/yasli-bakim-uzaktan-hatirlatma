using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Abstract;

public interface IHatirlatmaService
{
    Task<List<Hatirlatma>> YaslininHatirlatmalariAsync(int yasliId);
    // Bugün gerçekleşen (tekrar tipine göre) aktif hatırlatmalar.
    Task<List<Hatirlatma>> BugunkuHatirlatmalarAsync(int yasliId);
    Task<Hatirlatma?> GetirAsync(int id);
    Task EkleAsync(Hatirlatma hatirlatma);
    // Yalnızca düzenlenebilir alanlar güncellenir; hatırlatmanın sahibi (YasliId) değiştirilemez.
    Task GuncelleAsync(Hatirlatma hatirlatma);
    Task PasifeAlAsync(int id);
    Task AktifEtAsync(int id);
    Task SilAsync(int id);
}
