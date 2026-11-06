using System.ComponentModel.DataAnnotations;

namespace YasliBakim.Entities.Enums;

public enum AktiviteDurumu
{
    [Display(Name = "Yapıldı")] Tamamlandi = 1,     // zamanında onaylandı
    [Display(Name = "Atlandı")] Atlandi = 2,        // yaşlı "atla" dedi
    [Display(Name = "Geç yapıldı")] Gecikti = 3,    // onaylandı ama zamanında değil
    [Display(Name = "Kaçırıldı")] Kacirildi = 4     // süresi içinde hiç yanıt verilmedi (sistem işaretler)
}
