using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Abstract;

public interface IYasliService
{
    Task<List<Yasli>> TumunuGetirAsync();
    Task<Yasli?> GetirAsync(int id);
    Task EkleAsync(Yasli yasli);
}
