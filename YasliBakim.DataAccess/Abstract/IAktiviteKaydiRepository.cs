using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Abstract;

public interface IAktiviteKaydiRepository : IGenericRepository<AktiviteKaydi>
{
    Task<List<AktiviteKaydi>> GetSonKayitlarAsync(int yasliId, DateTime baslangic);

    // Verilen hatırlatmaların, planlanan zamanı [baslangic, bitis) aralığındaki onay kayıtları.
    Task<List<AktiviteKaydi>> GetOnaylarAsync(IReadOnlyCollection<int> hatirlatmaIdler, DateTime baslangic, DateTime bitis);

    // Bir yaşlının planlanan zamanı [baslangic, bitis) aralığındaki tüm hatırlatma onayları (uyum özeti için).
    Task<List<AktiviteKaydi>> GetYaslininOnaylariAsync(int yasliId, DateTime baslangic, DateTime bitis);
}
