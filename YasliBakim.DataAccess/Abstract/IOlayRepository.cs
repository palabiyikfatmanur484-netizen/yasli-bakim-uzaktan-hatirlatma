using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IOlayRepository : IGenericRepository<Olay>
{
    // Çözülmemiş olaylar: önce en yüksek seviye, sonra en yeni.
    // bakiciId verilirse yalnızca o bakıcıya atanmış yaşlıların olayları döner.
    Task<List<Olay>> GetAcikOlaylarAsync(int? bakiciId = null);
}
