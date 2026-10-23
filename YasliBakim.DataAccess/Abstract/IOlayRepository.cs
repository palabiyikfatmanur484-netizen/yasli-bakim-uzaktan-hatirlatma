using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IOlayRepository : IGenericRepository<Olay>
{
    // Çözülmemiş olaylar: önce en yüksek seviye, sonra en yeni.
    Task<List<Olay>> GetAcikOlaylarAsync();
}
