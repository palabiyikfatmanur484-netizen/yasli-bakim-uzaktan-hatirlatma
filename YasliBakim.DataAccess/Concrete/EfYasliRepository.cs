using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Context;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Concrete;

public class EfYasliRepository : EfGenericRepository<Yasli>, IYasliRepository
{
    public EfYasliRepository(YasliBakimDbContext context) : base(context)
    {
    }

    public Task<Yasli?> GetDetayAsync(int id) =>
        _dbSet
            .Include(y => y.Bakicilar).ThenInclude(yb => yb.Bakici)
            .Include(y => y.Hatirlatmalar)
            .AsSplitQuery()
            .FirstOrDefaultAsync(y => y.Id == id);

    public Task<List<Yasli>> AraAsync(string metin) =>
        _dbSet
            .Where(y => y.Ad.Contains(metin) || y.Soyad.Contains(metin))
            .OrderBy(y => y.Soyad).ThenBy(y => y.Ad)
            .AsNoTracking()
            .ToListAsync();
}
