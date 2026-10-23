using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IBakiciRepository : IGenericRepository<Bakici>
{
    Task<Bakici?> GetByEpostaAsync(string eposta);
    Task<List<Yasli>> GetYaslilariAsync(int bakiciId);
}
