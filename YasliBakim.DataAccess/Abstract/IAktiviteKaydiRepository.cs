using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IAktiviteKaydiRepository : IGenericRepository<AktiviteKaydi>
{
    Task<List<AktiviteKaydi>> GetSonKayitlarAsync(int yasliId, DateTime baslangic);
}
