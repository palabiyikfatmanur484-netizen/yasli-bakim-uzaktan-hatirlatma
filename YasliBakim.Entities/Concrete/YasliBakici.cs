namespace YasliBakim.Entities.Concrete;

/// <summary>
/// Yaşlı ile Bakıcı arasındaki N-N ilişkiyi çözen ara tablo.
/// Birincil anahtar: (YasliId, BakiciId) bileşik anahtarı.
/// </summary>
public class YasliBakici
{
    public int YasliId { get; set; }
    public Yasli Yasli { get; set; } = null!;

    public int BakiciId { get; set; }
    public Bakici Bakici { get; set; } = null!;

    public string? YakinlikDerecesi { get; set; }
    public bool BirincilSorumluMu { get; set; }
    public DateTime AtanmaTarihi { get; set; } = DateTime.Now;
}
