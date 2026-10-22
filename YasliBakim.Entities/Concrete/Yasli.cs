namespace YasliBakim.Entities.Concrete;

/// <summary>
/// Bakım alan yaşlı bireyin profili.
/// </summary>
public class Yasli : BaseEntity
{
    public string Ad { get; set; } = null!;
    public string Soyad { get; set; } = null!;
    public DateOnly DogumTarihi { get; set; }
    public string? Telefon { get; set; }
    public string? Adres { get; set; }
    public string? KronikHastaliklar { get; set; }
    public string? AcilDurumNotu { get; set; }

    public ICollection<YasliBakici> Bakicilar { get; set; } = new List<YasliBakici>();
    public ICollection<Hatirlatma> Hatirlatmalar { get; set; } = new List<Hatirlatma>();
    public ICollection<AktiviteKaydi> AktiviteKayitlari { get; set; } = new List<AktiviteKaydi>();
    public ICollection<Olay> Olaylar { get; set; } = new List<Olay>();
}
