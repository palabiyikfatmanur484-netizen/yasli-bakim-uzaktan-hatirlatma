using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Abstract;

public interface IOlayService
{
    Task<List<Olay>> AcikOlaylarAsync();
    Task<Olay?> GetirAsync(int id);
    Task<Olay> AcilButonAsync(int yasliId, string? aciklama = null);
    Task GorulduIsaretleAsync(int olayId, int bakiciId);
    Task CozulduIsaretleAsync(int olayId);
    Task SilAsync(int id);
}
