using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Context;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

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

    // UX_AktiviteKaydi_Hatirlatma_Planlanan indeksini kullanır.
    public Task<List<AktiviteKaydi>> GetOnaylarAsync(IReadOnlyCollection<int> hatirlatmaIdler, DateTime baslangic, DateTime bitis) =>
        _dbSet
            .Where(a => a.HatirlatmaId != null && hatirlatmaIdler.Contains(a.HatirlatmaId.Value)
                        && a.PlanlananZaman >= baslangic && a.PlanlananZaman < bitis)
            .AsNoTracking()
            .ToListAsync();

    public Task<List<AktiviteKaydi>> GetYaslininOnaylariAsync(int yasliId, DateTime baslangic, DateTime bitis) =>
        _dbSet
            .Include(a => a.Hatirlatma)
            .Where(a => a.YasliId == yasliId && a.Tur == AktiviteTuru.HatirlatmaOnayi
                        && a.PlanlananZaman >= baslangic && a.PlanlananZaman < bitis)
            .AsNoTracking()
            .ToListAsync();
}
