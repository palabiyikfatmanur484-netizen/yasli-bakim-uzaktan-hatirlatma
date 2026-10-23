using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Abstract;

public interface IYasliService
{
    Task<List<Yasli>> TumunuGetirAsync();
    Task<Yasli?> GetirAsync(int id);
    Task<Yasli?> DetayGetirAsync(int id);
    Task<List<Yasli>> AraAsync(string metin);
    Task EkleAsync(Yasli yasli);
    Task GuncelleAsync(Yasli yasli);
    Task SilAsync(int id);
}
