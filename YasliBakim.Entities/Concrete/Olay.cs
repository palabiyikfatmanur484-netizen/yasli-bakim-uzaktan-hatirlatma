using YasliBakim.Entities.Enums;

namespace YasliBakim.Entities.Concrete;

/// <summary>
/// Yakınlara bildirilen olay/alarm: acil buton, kaçırılan hatırlatma veya anomali.
/// </summary>
public class Olay : BaseEntity
{
    public int YasliId { get; set; }
    public Yasli Yasli { get; set; } = null!;

    // Olayı tetikleyen aktivite kaydı (ör. atlanan ilaç). Acil butonda boş kalır.
    public int? AktiviteKaydiId { get; set; }
    public AktiviteKaydi? AktiviteKaydi { get; set; }

    public int? IlgilenenBakiciId { get; set; }
    public Bakici? IlgilenenBakici { get; set; }

    public OlayTuru Tur { get; set; }
    public OlaySeviyesi Seviye { get; set; }
    public string? Aciklama { get; set; }
    public DateTime OlusmaZamani { get; set; } = DateTime.Now;
    public OlayDurumu Durum { get; set; } = OlayDurumu.Acik;
    public DateTime? CozulmeZamani { get; set; }
}
