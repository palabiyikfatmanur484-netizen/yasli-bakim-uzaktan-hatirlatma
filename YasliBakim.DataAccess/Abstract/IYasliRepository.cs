using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IYasliRepository : IGenericRepository<Yasli>
{
    // Profil sayfası: bakıcılar ve hatırlatmalarla birlikte.
    Task<Yasli?> GetDetayAsync(int id);
    Task<List<Yasli>> AraAsync(string metin);
}
