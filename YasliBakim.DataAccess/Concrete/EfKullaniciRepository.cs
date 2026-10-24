using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Context;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Concrete;

public class EfKullaniciRepository : EfGenericRepository<Kullanici>, IKullaniciRepository
{
    public EfKullaniciRepository(YasliBakimDbContext context) : base(context)
    {
    }

    // UX_Kullanici_Eposta indeksini kullanır.
    public Task<Kullanici?> GetByEpostaAsync(string eposta) =>
        _dbSet.FirstOrDefaultAsync(k => k.Eposta == eposta);

    public Task<List<Kullanici>> GetTumuDetayliAsync() =>
        _dbSet
            .Include(k => k.Bakici)
            .Include(k => k.Yasli)
            .OrderBy(k => k.Rol).ThenBy(k => k.Eposta)
            .AsNoTracking()
            .ToListAsync();
}
