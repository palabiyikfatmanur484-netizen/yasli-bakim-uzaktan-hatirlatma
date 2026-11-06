using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.DataAccess.Abstract;

public interface IAktiviteKaydiRepository : IGenericRepository<AktiviteKaydi>
{
    // Yaşlının zaman çizelgesi: KayitZamani [baslangic, bitis) aralığında, en yeni önce. tur verilirse yalnızca o tür.
    Task<List<AktiviteKaydi>> GetGecmisAsync(int yasliId, DateTime baslangic, DateTime bitis, AktiviteTuru? tur = null);

    // Verilen günün (varsa en son) günlük durum kaydı. Güncellenebilsin diye izlenen (tracked) nesne döner.
    Task<AktiviteKaydi?> GetGunlukDurumAsync(int yasliId, DateOnly gun);

    // Verilen hatırlatmaların, planlanan zamanı [baslangic, bitis) aralığındaki onay kayıtları.
    Task<List<AktiviteKaydi>> GetOnaylarAsync(IReadOnlyCollection<int> hatirlatmaIdler, DateTime baslangic, DateTime bitis);

    // Bir yaşlının planlanan zamanı [baslangic, bitis) aralığındaki tüm hatırlatma onayları (uyum özeti için).
    Task<List<AktiviteKaydi>> GetYaslininOnaylariAsync(int yasliId, DateTime baslangic, DateTime bitis);
}
