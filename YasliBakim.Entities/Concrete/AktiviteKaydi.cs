using YasliBakim.Entities.Enums;

namespace YasliBakim.Entities.Concrete;

/// <summary>
/// Yaşlının yaptığı işlemlerin kaydı: bir hatırlatmanın onayı veya günlük durum bildirimi.
/// </summary>
public class AktiviteKaydi : BaseEntity
{
    public int YasliId { get; set; }
    public Yasli Yasli { get; set; } = null!;

    // Günlük durum kayıtlarında boş kalır.
    public int? HatirlatmaId { get; set; }
    public Hatirlatma? Hatirlatma { get; set; }

    public AktiviteTuru Tur { get; set; }
    public DateTime KayitZamani { get; set; } = DateTime.Now;
    public AktiviteDurumu? Durum { get; set; }
    public RuhHali? RuhHali { get; set; }
    public string? Not { get; set; }
}
