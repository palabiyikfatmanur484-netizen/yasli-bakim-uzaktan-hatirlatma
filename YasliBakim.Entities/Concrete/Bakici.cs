using YasliBakim.Entities.Enums;

namespace YasliBakim.Entities.Concrete;

/// <summary>
/// Yaşlıyı takip eden kişi: profesyonel bakıcı veya aile yakını (Yakın paneli kullanıcısı).
/// </summary>
public class Bakici : BaseEntity
{
    public string Ad { get; set; } = null!;
    public string Soyad { get; set; } = null!;
    public string Eposta { get; set; } = null!;
    public string Telefon { get; set; } = null!;
    public BakiciRolu Rol { get; set; }

    public ICollection<YasliBakici> Yaslilar { get; set; } = new List<YasliBakici>();
    public ICollection<Hatirlatma> OlusturduguHatirlatmalar { get; set; } = new List<Hatirlatma>();
    public ICollection<Olay> IlgilendigiOlaylar { get; set; } = new List<Olay>();
}
