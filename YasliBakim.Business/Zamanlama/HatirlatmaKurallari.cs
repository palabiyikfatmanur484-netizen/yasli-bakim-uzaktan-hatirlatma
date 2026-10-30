using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Zamanlama;

/// <summary>
/// Onay / kaçırma iş kuralları. Süreler tek yerde tutulur; değiştirmek için yalnızca burası düzenlenir.
///
///   planlanan-60dk ─── planlanan ───── +30dk ─────────── +120dk
///   │  onay açılır    │   Tamamlandı    │    Gecikti       │  Kaçırıldı (sistem işaretler)
/// </summary>
public static class HatirlatmaKurallari
{
    // Akşam ilacının sabahtan "alındı" işaretlenmesini engeller.
    public static readonly TimeSpan ErkenOnaySuresi = TimeSpan.FromMinutes(60);

    // Bu süre içindeki onay "zamanında" sayılır.
    public static readonly TimeSpan ZamanindaSuresi = TimeSpan.FromMinutes(30);

    // Bu süre geçince yanıt verilmemiş tekrar "kaçırıldı" olur ve artık onaylanamaz.
    public static readonly TimeSpan KacirmaSuresi = TimeSpan.FromHours(2);

    // Yalnızca sağlık açısından kritik türlerin kaçırılması/atlanması yakınlara alarm üretir.
    // Su, yemek, egzersiz kaydedilir ama alarm üretmez (yakınların sürekli bildirimle yorulmaması için).
    public static bool BildirimGerektirir(HatirlatmaTuru tur) => tur is HatirlatmaTuru.Ilac or HatirlatmaTuru.Randevu;

    public static OlaySeviyesi AlarmSeviyesi(HatirlatmaTuru tur) =>
        tur == HatirlatmaTuru.Ilac ? OlaySeviyesi.Yuksek : OlaySeviyesi.Orta;

    public static bool OnayPenceresiAcikMi(DateTime planlanan, DateTime simdi) =>
        simdi >= planlanan - ErkenOnaySuresi && simdi < planlanan + KacirmaSuresi;

    public static bool KacirildiMi(DateTime planlanan, DateTime simdi) => simdi >= planlanan + KacirmaSuresi;

    // "Aldım" yanıtının sonucu: zamanında mı geç mi?
    public static AktiviteDurumu AlindiDurumu(DateTime planlanan, DateTime onayZamani) =>
        onayZamani <= planlanan + ZamanindaSuresi ? AktiviteDurumu.Tamamlandi : AktiviteDurumu.Gecikti;
}
