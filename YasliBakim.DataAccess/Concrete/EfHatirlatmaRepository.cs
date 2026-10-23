using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Context;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Concrete;

public class EfHatirlatmaRepository : EfGenericRepository<Hatirlatma>, IHatirlatmaRepository
{
    public EfHatirlatmaRepository(YasliBakimDbContext context) : base(context)
    {
    }

    // IX_Hatirlatma_Yasli_Aktif_Saat indeksini kullanır.
    public Task<List<Hatirlatma>> GetGecerliHatirlatmalarAsync(int yasliId, DateOnly tarih) =>
        _dbSet
            .Where(h => h.YasliId == yasliId && h.AktifMi
                        && h.BaslangicTarihi <= tarih
                        && (h.BitisTarihi == null || h.BitisTarihi >= tarih))
            .OrderBy(h => h.Saat)
            .AsNoTracking()
            .ToListAsync();
}
