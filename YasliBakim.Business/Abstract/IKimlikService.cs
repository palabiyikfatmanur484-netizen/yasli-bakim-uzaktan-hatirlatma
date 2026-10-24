using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Abstract;

public record GirisSonucu(bool Basarili, Kullanici? Kullanici, string? Hata);

public interface IKimlikService
{
    // Herkese açık kayıt yalnızca bakıcı/aile yakını hesabı açar; rol formdan seçilemez.
    Task<Kullanici> BakiciKayitAsync(string ad, string soyad, string eposta, string telefon, string parola);
    Task<GirisSonucu> GirisAsync(string eposta, string parola);
    Task<List<Kullanici>> TumKullanicilarAsync();
    Task KilidiKaldirAsync(int kullaniciId);
}
