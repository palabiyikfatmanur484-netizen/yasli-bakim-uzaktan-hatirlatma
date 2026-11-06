using System.ComponentModel.DataAnnotations;

namespace YasliBakim.Entities.Enums;

public enum AktiviteTuru
{
    [Display(Name = "Hatırlatma onayı")] HatirlatmaOnayi = 1,
    [Display(Name = "Günlük durum")] GunlukDurum = 2,
    // Yaşlının ya da bakıcısının kendiliğinden kaydettiği aktivite (yürüyüş, ziyaret vb.).
    [Display(Name = "Aktivite")] Aktivite = 3
}
