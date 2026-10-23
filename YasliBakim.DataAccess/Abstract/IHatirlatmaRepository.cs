using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IHatirlatmaRepository : IGenericRepository<Hatirlatma>
{
    // Verilen tarihte geçerli olan aktif hatırlatmalar, saate göre sıralı.
    Task<List<Hatirlatma>> GetGecerliHatirlatmalarAsync(int yasliId, DateOnly tarih);
}
