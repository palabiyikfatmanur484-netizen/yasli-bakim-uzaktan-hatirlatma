using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Abstract;

public interface IBakiciService
{
    Task<List<Bakici>> TumunuGetirAsync();
    Task<Bakici?> GetirAsync(int id);
    Task<List<Yasli>> YaslilariGetirAsync(int bakiciId);
    Task EkleAsync(Bakici bakici);
    Task GuncelleAsync(Bakici bakici);
    Task SilAsync(int id);

    // N-N ilişki: yaşlı-bakıcı eşleştirmesi
    Task YasliyaAtaAsync(int yasliId, int bakiciId, string? yakinlikDerecesi, bool birincilSorumluMu);
    Task AtamayiKaldirAsync(int yasliId, int bakiciId);
}
