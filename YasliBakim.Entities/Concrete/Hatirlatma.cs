using YasliBakim.Entities.Enums;

namespace YasliBakim.Entities.Concrete;

/// <summary>
/// İlaç, randevu, su içme vb. zamanlanmış hatırlatma.
/// </summary>
public class Hatirlatma : BaseEntity
{
    public int YasliId { get; set; }
    public Yasli Yasli { get; set; } = null!;

    public int? OlusturanBakiciId { get; set; }
    public Bakici? OlusturanBakici { get; set; }

    public string Baslik { get; set; } = null!;
    public string? Aciklama { get; set; }
    public HatirlatmaTuru Tur { get; set; }
    public TimeOnly Saat { get; set; }
    public TekrarTipi Tekrar { get; set; }
    public DateOnly BaslangicTarihi { get; set; }
    public DateOnly? BitisTarihi { get; set; }
    public bool AktifMi { get; set; } = true;

    public ICollection<AktiviteKaydi> AktiviteKayitlari { get; set; } = new List<AktiviteKaydi>();
}
