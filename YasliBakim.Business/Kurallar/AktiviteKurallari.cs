using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Kurallar;

/// <summary>
/// Günlük durum ve aktivite kaydı iş kuralları. Sınırlar tek yerde tutulur.
/// </summary>
public static class AktiviteKurallari
{
    // Aktivite en fazla 2 gün geriye dönük girilebilir: haftalar sonra hatırlanan kayıt güvenilir değildir
    // ve geçmişin sonradan "düzeltilmesini" zorlaştırır.
    public static readonly TimeSpan GeriyeDonukSinir = TimeSpan.FromHours(48);

    // Cihaz saati ile sunucu saati arasındaki küçük farka tolerans; daha ilerisi "gelecek" sayılır.
    public static readonly TimeSpan IleriTolerans = TimeSpan.FromMinutes(5);

    // Yanlış girilen aktivite, giren kişi tarafından 24 saat içinde silinebilir.
    public static readonly TimeSpan SilmeSuresi = TimeSpan.FromHours(24);

    public const int EnKisaSure = 1;
    public const int EnUzunSure = 480;          // 8 saat
    public const int EnYuksekAgri = 10;
    public const int NotUzunlugu = 500;

    // Geçmiş ekranında tek seferde gösterilen en uzun aralık (sorgu boyutu sınırlı kalsın).
    public const int GecmisEnFazlaGun = 31;

    // Ağrı bu seviyede ya da üstündeyse yakınlara bildirilir.
    public const int AgriOrtaEsigi = 7;
    public const int AgriYuksekEsigi = 9;

    /// <summary>
    /// Günlük durumun yakınlara olay (anomali) üretip üretmeyeceği ve seviyesi. null = normal, olay yok.
    /// Ruh hali ve ağrıdan hangisi daha kötüyse o seviye alınır.
    /// </summary>
    public static OlaySeviyesi? AnomaliSeviyesi(RuhHali ruhHali, int? agri)
    {
        var ruh = ruhHali switch
        {
            RuhHali.CokKotu => OlaySeviyesi.Yuksek,
            RuhHali.Kotu => OlaySeviyesi.Orta,
            _ => (OlaySeviyesi?)null,
        };
        var agriSeviyesi = agri switch
        {
            >= AgriYuksekEsigi => OlaySeviyesi.Yuksek,
            >= AgriOrtaEsigi => OlaySeviyesi.Orta,
            _ => (OlaySeviyesi?)null,
        };
        if (ruh is null || agriSeviyesi is null)
            return ruh ?? agriSeviyesi;
        return ruh > agriSeviyesi ? ruh : agriSeviyesi;
    }

    /// <summary>
    /// Yalnızca aktivite türündeki kayıt, onu giren kişi tarafından ve süresi içinde silinebilir.
    /// bakiciId: silmeye çalışan bakıcı; yaşlının kendisi için null.
    /// Hatırlatma onayları (uyum verisi) ve günlük durum (olaya kaynak olabilir) silinmez.
    /// </summary>
    public static bool SilinebilirMi(AktiviteKaydi kayit, int? bakiciId, DateTime simdi) =>
        kayit.Tur == AktiviteTuru.Aktivite
        && kayit.KaydedenBakiciId == bakiciId
        && simdi - kayit.OlusturmaTarihi <= SilmeSuresi;
}
