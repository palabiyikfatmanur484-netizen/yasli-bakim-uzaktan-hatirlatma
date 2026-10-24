using YasliBakim.Entities.Enums;

namespace yaslibakimapp.Guvenlik;

// [Authorize(Roles = ...)] içinde sabit (const) gerektiği için enum adları burada metin olarak tutulur.
public static class Roller
{
    public const string Yonetici = nameof(KullaniciRolu.Yonetici);
    public const string Bakici = nameof(KullaniciRolu.Bakici);
    public const string Yasli = nameof(KullaniciRolu.Yasli);

    public const string YoneticiVeyaBakici = Yonetici + "," + Bakici;
}
