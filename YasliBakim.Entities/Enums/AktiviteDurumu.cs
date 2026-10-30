namespace YasliBakim.Entities.Enums;

public enum AktiviteDurumu
{
    Tamamlandi = 1,   // zamanında onaylandı
    Atlandi = 2,      // yaşlı "atla" dedi
    Gecikti = 3,      // onaylandı ama zamanında değil
    Kacirildi = 4     // süresi içinde hiç yanıt verilmedi (sistem işaretler)
}
