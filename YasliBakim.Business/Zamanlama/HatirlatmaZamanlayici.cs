using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Zamanlama;

/// <summary>
/// Tekrarlayan hatırlatmanın hangi günlerde ve saatlerde gerçekleştiğini hesaplar.
/// Veritabanına tek tek "tekrar" satırı yazılmaz; kural (Saat + Tekrar + Başlangıç/Bitiş) saklanır,
/// tekrarlar ihtiyaç anında bu sınıfla üretilir. Veritabanı ve saat bağımlılığı yoktur, saf hesaplamadır.
/// </summary>
public static class HatirlatmaZamanlayici
{
    public static bool GerceklesirMi(Hatirlatma h, DateOnly tarih)
    {
        if (!h.AktifMi || tarih < h.BaslangicTarihi || (h.BitisTarihi is { } bitis && tarih > bitis))
            return false;

        return h.Tekrar switch
        {
            TekrarTipi.Tek => tarih == h.BaslangicTarihi,
            TekrarTipi.Gunluk => true,
            // Haftalık: başlangıç tarihi hangi güne denk geliyorsa her hafta o gün.
            TekrarTipi.Haftalik => tarih.DayOfWeek == h.BaslangicTarihi.DayOfWeek,
            // Aylık: her ay başlangıçtaki gün; 31'i olmayan aylarda ayın son günü.
            TekrarTipi.Aylik => tarih.Day == Math.Min(h.BaslangicTarihi.Day, DateTime.DaysInMonth(tarih.Year, tarih.Month)),
            _ => false,
        };
    }

    public static DateTime PlanlananZaman(Hatirlatma h, DateOnly tarih) => tarih.ToDateTime(h.Saat);

    // [baslangic, bitis] günleri arasındaki tüm tekrarların planlanan zamanları.
    public static IEnumerable<DateTime> Tekrarlar(Hatirlatma h, DateOnly baslangic, DateOnly bitis)
    {
        for (var gun = baslangic; gun <= bitis; gun = gun.AddDays(1))
            if (GerceklesirMi(h, gun))
                yield return PlanlananZaman(h, gun);
    }

    // Verilen andan sonraki ilk tekrar; bir yıl içinde yoksa (ör. süresi bitmiş) null.
    public static DateTime? SonrakiTekrar(Hatirlatma h, DateTime simdi)
    {
        var bugun = DateOnly.FromDateTime(simdi);
        return Tekrarlar(h, bugun, bugun.AddDays(366)).Cast<DateTime?>().FirstOrDefault(t => t > simdi);
    }
}
