using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Abstract;

public interface IHatirlatmaService
{
    Task<List<Hatirlatma>> YaslininHatirlatmalariAsync(int yasliId);
    Task<List<Hatirlatma>> BugunkuHatirlatmalarAsync(int yasliId);
    Task<Hatirlatma?> GetirAsync(int id);
    Task EkleAsync(Hatirlatma hatirlatma);
    Task GuncelleAsync(Hatirlatma hatirlatma);
    Task PasifeAlAsync(int id);
    Task SilAsync(int id);
}
