using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Abstract;

// Ekranda gösterilen durum (veritabanında saklanmaz; kayıt + saatten hesaplanır).
public enum PlanDurumu
{
    Bekliyor,          // onay penceresi henüz açılmadı
    OnayBekleniyor,    // şu an onaylanabilir
    Tamamlandi,
    Gecikti,
    Atlandi,
    Kacirildi
}

public record PlanOgesi(Hatirlatma Hatirlatma, DateTime PlanlananZaman, PlanDurumu Durum, AktiviteKaydi? Kayit)
{
    public bool OnaylanabilirMi => Durum == PlanDurumu.OnayBekleniyor;
}

public record UyumOzeti(int Toplam, int Zamaninda, int Gec, int Atlanan, int Kacirilan)
{
    // Alınan (zamanında + geç) dozların yanıtlanması gereken tüm dozlara oranı.
    public double? Oran => Toplam == 0 ? null : 100.0 * (Zamaninda + Gec) / Toplam;
}

/// <summary>
/// Hatırlatmaların günlük takibi: plan, onay/geri bildirim ve kaçırılanların işaretlenmesi.
/// </summary>
public interface IHatirlatmaTakipService
{
    Task<List<PlanOgesi>> GunlukPlanAsync(int yasliId, DateOnly tarih);

    // yasliId oturumdan gelir; hatırlatma o yaşlıya ait değilse işlem reddedilir.
    Task<AktiviteKaydi> YanitlaAsync(int yasliId, int hatirlatmaId, DateTime planlananZaman, bool alindi, string? not = null);

    // Süresi geçtiği hâlde yanıtlanmamış tekrarları "Kaçırıldı" yapar; döndürdüğü değer işaretlenen sayısıdır.
    Task<int> KacirilanlariIsleAsync();

    Task<UyumOzeti> UyumOzetiAsync(int yasliId, int gun = 7);
}
