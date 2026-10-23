using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Context;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Concrete;

public class EfBakiciRepository : EfGenericRepository<Bakici>, IBakiciRepository
{
    public EfBakiciRepository(YasliBakimDbContext context) : base(context)
    {
    }

    public Task<Bakici?> GetByEpostaAsync(string eposta) =>
        _dbSet.FirstOrDefaultAsync(b => b.Eposta == eposta);

    public Task<List<Yasli>> GetYaslilariAsync(int bakiciId) =>
        _context.YasliBakicilar
            .Where(yb => yb.BakiciId == bakiciId)
            .Select(yb => yb.Yasli)
            .OrderBy(y => y.Soyad).ThenBy(y => y.Ad)
            .AsNoTracking()
            .ToListAsync();
}
