using YasliBakim.Entities.Enums;

namespace YasliBakim.Entities.Concrete;

/// <summary>
/// Yaşlının zaman çizelgesindeki bir kayıt: hatırlatma onayı, günlük durum bildirimi veya aktivite.
/// Hangi alanların dolu olacağı türe göre değişir (CK_AktiviteKaydi_Tur).
/// </summary>
public class AktiviteKaydi : BaseEntity
{
    public int YasliId { get; set; }
    public Yasli Yasli { get; set; } = null!;

    // Yalnızca hatırlatma onayında dolu.
    public int? HatirlatmaId { get; set; }
    public Hatirlatma? Hatirlatma { get; set; }

    // Kaydı bakıcı girdiyse o bakıcı; boşsa yaşlının kendisi (ya da sistem) girmiştir.
    public int? KaydedenBakiciId { get; set; }
    public Bakici? KaydedenBakici { get; set; }

    public AktiviteTuru Tur { get; set; }

    // Hatırlatmanın hangi tekrarına ait olduğu (ör. 30.10.2026 08:00 dozu). Diğer türlerde boş kalır.
    // (HatirlatmaId, PlanlananZaman) benzersizdir: aynı doz iki kez onaylanamaz.
    public DateTime? PlanlananZaman { get; set; }

    // Zaman çizelgesindeki yeri: onay anı, durum bildirimi anı ya da aktivitenin yapıldığı an.
    // Aktivite geriye dönük girilebildiği için girişin yapıldığı an ayrıca OlusturmaTarihi'nde durur.
    public DateTime KayitZamani { get; set; } = DateTime.Now;

    // Hatırlatma onayı
    public AktiviteDurumu? Durum { get; set; }

    // Günlük durum
    public RuhHali? RuhHali { get; set; }
    public int? AgriSeviyesi { get; set; }      // 0 (yok) – 10 (dayanılmaz)

    // Aktivite
    public AktiviteKategorisi? Kategori { get; set; }
    public int? SureDakika { get; set; }

    public string? Not { get; set; }
}
