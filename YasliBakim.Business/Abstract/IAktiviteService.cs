using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Abstract;

// Aktivite girişi. KaydedenBakiciId: bakıcı girdiyse onun Id'si, yaşlının kendisi girdiyse null (oturumdan gelir).
public record AktiviteGirdisi(int YasliId, int? KaydedenBakiciId, AktiviteKategorisi Kategori, int SureDakika,
    DateTime Zaman, string? Not = null);

// Geçmiş ekranındaki bir günün özeti (veritabanında saklanmaz; kayıtlardan hesaplanır).
public record GunOzeti(DateOnly Gun, AktiviteKaydi? Durum, int AktiviteSayisi, int AktiviteDakika, int AlinanDoz, int YanitlananDoz);

/// <summary>
/// Günlük durum ve aktivite kaydı. Hatırlatma onayları IHatirlatmaTakipService içindedir;
/// burada hepsi yaşlının ortak zaman çizelgesinde (geçmiş) birlikte gösterilir.
/// </summary>
public interface IAktiviteService
{
    Task<AktiviteKaydi?> BugunkuDurumAsync(int yasliId);

    // Günde bir durum kaydı tutulur: aynı gün tekrar bildirilirse mevcut kayıt güncellenir.
    // Kötü ruh hali veya yüksek ağrı yakınlara olay (anomali) üretir.
    Task<AktiviteKaydi> GunlukDurumBildirAsync(int yasliId, RuhHali ruhHali, int? agriSeviyesi = null, string? not = null);

    Task<AktiviteKaydi> AktiviteEkleAsync(AktiviteGirdisi girdi);

    // yasliId ve bakiciId oturumdan gelir; kayıt o yaşlıya ait değilse veya başkası girdiyse reddedilir.
    Task AktiviteSilAsync(int kayitId, int yasliId, int? bakiciId);

    // [baslangic, bitis] günleri (ikisi dahil), en yeni önce. Aralık en fazla 31 gün.
    Task<List<AktiviteKaydi>> GecmisAsync(int yasliId, DateOnly baslangic, DateOnly bitis, AktiviteTuru? tur = null);

    // Aynı aralığın gün gün özeti, en yeni gün önce.
    Task<List<GunOzeti>> GunlukOzetAsync(int yasliId, DateOnly baslangic, DateOnly bitis);
}
