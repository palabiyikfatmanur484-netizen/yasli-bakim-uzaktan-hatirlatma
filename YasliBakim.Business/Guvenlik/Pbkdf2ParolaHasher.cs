using System.Security.Cryptography;

namespace YasliBakim.Business.Guvenlik;

/// <summary>
/// PBKDF2-HMAC-SHA256 ile tuzlu parola hash'i (.NET'in kendi kriptografi kütüphanesi, ek paket yok).
/// Saklanan biçim: "PBKDF2$iterasyon$tuz(base64)$hash(base64)".
/// İterasyon sayısı OWASP Password Storage Cheat Sheet önerisidir (PBKDF2-HMAC-SHA256 için 600.000).
/// </summary>
public class Pbkdf2ParolaHasher : IParolaHasher
{
    private const string Onek = "PBKDF2";
    private const int TuzUzunlugu = 16;   // 128 bit
    private const int HashUzunlugu = 32;  // 256 bit
    private static readonly HashAlgorithmName Algoritma = HashAlgorithmName.SHA256;

    private readonly int _iterasyon;

    public Pbkdf2ParolaHasher(int iterasyon = 600_000)
    {
        _iterasyon = iterasyon;
    }

    public string Hashle(string parola)
    {
        // Her parola için rastgele tuz: aynı parolayı kullanan iki kullanıcının hash'i farklı olur,
        // önceden hesaplanmış tablolar (rainbow table) işe yaramaz.
        var tuz = RandomNumberGenerator.GetBytes(TuzUzunlugu);
        var hash = Rfc2898DeriveBytes.Pbkdf2(parola, tuz, _iterasyon, Algoritma, HashUzunlugu);
        return $"{Onek}${_iterasyon}${Convert.ToBase64String(tuz)}${Convert.ToBase64String(hash)}";
    }

    public bool Dogrula(string parola, string kayitliHash)
    {
        var parcalar = kayitliHash.Split('$');
        if (parcalar.Length != 4 || parcalar[0] != Onek || !int.TryParse(parcalar[1], out var iterasyon))
            return false;

        var tuz = Convert.FromBase64String(parcalar[2]);
        var beklenen = Convert.FromBase64String(parcalar[3]);

        // Kayıttaki tuz ve iterasyonla girilen parolayı tekrar hash'leyip karşılaştırırız.
        var hesaplanan = Rfc2898DeriveBytes.Pbkdf2(parola, tuz, iterasyon, Algoritma, beklenen.Length);

        // Sabit zamanlı karşılaştırma: kaç baytın eşleştiği süreden anlaşılamaz (timing attack).
        return CryptographicOperations.FixedTimeEquals(hesaplanan, beklenen);
    }
}
