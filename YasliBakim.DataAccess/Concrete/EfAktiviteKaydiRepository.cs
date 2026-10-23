using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Context;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Concrete;

public class EfAktiviteKaydiRepository : EfGenericRepository<AktiviteKaydi>, IAktiviteKaydiRepository
{
    public EfAktiviteKaydiRepository(YasliBakimDbContext context) : base(context)
    {
    }

    // IX_AktiviteKaydi_Yasli_Zaman indeksini kullanır.
    public Task<List<AktiviteKaydi>> GetSonKayitlarAsync(int yasliId, DateTime baslangic) =>
        _dbSet
            .Include(a => a.Hatirlatma)
            .Where(a => a.YasliId == yasliId && a.KayitZamani >= baslangic)
            .OrderByDescending(a => a.KayitZamani)
            .AsNoTracking()
            .ToListAsync();
}
