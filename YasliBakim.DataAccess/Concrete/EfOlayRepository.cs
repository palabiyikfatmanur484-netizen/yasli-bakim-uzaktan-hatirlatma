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
    public Task<List<Olay>> GetAcikOlaylarAsync(int? bakiciId = null)
    {
        var sorgu = _dbSet
            .Include(o => o.Yasli)
            .Include(o => o.IlgilenenBakici)
            .Where(o => o.Durum != OlayDurumu.Cozuldu);

        if (bakiciId is not null)
            sorgu = sorgu.Where(o => o.Yasli.Bakicilar.Any(yb => yb.BakiciId == bakiciId));

        return sorgu
            .OrderByDescending(o => o.Seviye).ThenByDescending(o => o.OlusmaZamani)
            .AsNoTracking()
            .ToListAsync();
    }
}
