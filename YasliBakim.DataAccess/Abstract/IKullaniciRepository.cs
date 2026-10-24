using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IKullaniciRepository : IGenericRepository<Kullanici>
{
    Task<Kullanici?> GetByEpostaAsync(string eposta);
    // Yönetim ekranı: bağlı bakıcı/yaşlı adlarıyla birlikte.
    Task<List<Kullanici>> GetTumuDetayliAsync();
}
