using YasliBakim.Entities.Enums;

namespace YasliBakim.Entities.Concrete;

/// <summary>
/// Sisteme giriş yapan hesap. Kimlik bilgisi (e-posta, parola hash'i) alan varlıklardan (Bakici, Yasli) ayrı tutulur.
/// Bakıcı hesabı bir Bakici kaydına, yaşlı hesabı bir Yasli kaydına bağlıdır; yöneticinin bağlı kaydı yoktur.
/// </summary>
public class Kullanici : BaseEntity
{
    public string Eposta { get; set; } = null!;

    // Parolanın kendisi değil, "PBKDF2$iterasyon$tuz$hash" biçiminde tuzlu hash'i saklanır.
    public string ParolaHash { get; set; } = null!;

    public KullaniciRolu Rol { get; set; }

    public int? BakiciId { get; set; }
    public Bakici? Bakici { get; set; }

    public int? YasliId { get; set; }
    public Yasli? Yasli { get; set; }

    public bool AktifMi { get; set; } = true;

    // Kaba kuvvet (brute force) koruması
    public int BasarisizGirisSayisi { get; set; }
    public DateTime? KilitBitisZamani { get; set; }

    public DateTime? SonGirisZamani { get; set; }
}
