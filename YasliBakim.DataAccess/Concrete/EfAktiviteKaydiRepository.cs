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
    public Task<List<AktiviteKaydi>> GetGecmisAsync(int yasliId, DateTime baslangic, DateTime bitis, AktiviteTuru? tur = null)
    {
        var sorgu = _dbSet
            .Include(a => a.Hatirlatma)
            .Include(a => a.KaydedenBakici)
            .Where(a => a.YasliId == yasliId && a.KayitZamani >= baslangic && a.KayitZamani < bitis);
        if (tur is not null)
            sorgu = sorgu.Where(a => a.Tur == tur);

        return sorgu
            .OrderByDescending(a => a.KayitZamani)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<AktiviteKaydi?> GetGunlukDurumAsync(int yasliId, DateOnly gun)
    {
        var gunBasi = gun.ToDateTime(TimeOnly.MinValue);
        return _dbSet
            .Where(a => a.YasliId == yasliId && a.Tur == AktiviteTuru.GunlukDurum
                        && a.KayitZamani >= gunBasi && a.KayitZamani < gunBasi.AddDays(1))
            .OrderByDescending(a => a.KayitZamani)
            .FirstOrDefaultAsync();
    }

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
