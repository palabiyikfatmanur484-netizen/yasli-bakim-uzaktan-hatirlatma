using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Context;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.DataAccess.Concrete;

public class EfOlayRepository : EfGenericRepository<Olay>, IOlayRepository
{
    public EfOlayRepository(YasliBakimDbContext context) : base(context)
    {
    }

    // IX_Olay_Durum_Seviye indeksini kullanır.
    public Task<List<Olay>> GetAcikOlaylarAsync() =>
        _dbSet
            .Include(o => o.Yasli)
            .Include(o => o.IlgilenenBakici)
            .Where(o => o.Durum != OlayDurumu.Cozuldu)
            .OrderByDescending(o => o.Seviye).ThenByDescending(o => o.OlusmaZamani)
            .AsNoTracking()
            .ToListAsync();
}
