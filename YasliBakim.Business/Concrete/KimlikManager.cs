using YasliBakim.Business.Abstract;
using YasliBakim.Business.Guvenlik;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Concrete;

public class KimlikManager : IKimlikService
{
    public const int MaksimumHataliGiris = 5;
    public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);

    // Kullanıcı var mı yok mu, yanıt mesajından anlaşılmasın diye her durumda aynı mesaj.
    private const string GenelHata = "E-posta veya parola hatalı.";

    private readonly IKullaniciRepository _repository;
    private readonly IBakiciRepository _bakiciRepository;
    private readonly IParolaHasher _hasher;

    // Olmayan e-postada da bir hash doğrulaması yapılır; böylece yanıt süresi kullanıcının varlığını ele vermez.
    private readonly Lazy<string> _sahteHash;

    public KimlikManager(IKullaniciRepository repository, IBakiciRepository bakiciRepository, IParolaHasher hasher)
    {
        _repository = repository;
        _bakiciRepository = bakiciRepository;
        _hasher = hasher;
        _sahteHash = new Lazy<string>(() => _hasher.Hashle(Guid.NewGuid().ToString()));
    }

    public async Task<Kullanici> BakiciKayitAsync(string ad, string soyad, string eposta, string telefon, string parola)
    {
        eposta = eposta.Trim().ToLowerInvariant();
        ParolaKuraliniKontrolEt(parola);

        if (await _repository.AnyAsync(k => k.Eposta == eposta) || await _bakiciRepository.AnyAsync(b => b.Eposta == eposta))
            throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı.");

        var kullanici = new Kullanici
        {
            Eposta = eposta,
            ParolaHash = _hasher.Hashle(parola),
            Rol = KullaniciRolu.Bakici,
            Bakici = new Bakici
            {
                Ad = ad.Trim(), Soyad = soyad.Trim(), Eposta = eposta, Telefon = telefon.Trim(),
                Rol = BakiciRolu.AileYakini,
            },
        };

        // Bakici ve Kullanici aynı SaveChanges ile (tek transaction) eklenir.
        await _repository.AddAsync(kullanici);
        await _repository.SaveAsync();
        return kullanici;
    }

    public async Task<GirisSonucu> GirisAsync(string eposta, string parola)
    {
        eposta = eposta.Trim().ToLowerInvariant();
        var kullanici = await _repository.GetByEpostaAsync(eposta);

        if (kullanici is null)
        {
            _hasher.Dogrula(parola, _sahteHash.Value);
            return new GirisSonucu(false, null, GenelHata);
        }

        if (kullanici.KilitBitisZamani > DateTime.Now)
            return new GirisSonucu(false, null,
                $"Çok fazla hatalı deneme. Hesap {kullanici.KilitBitisZamani:HH:mm} saatine kadar kilitli.");

        if (!_hasher.Dogrula(parola, kullanici.ParolaHash))
        {
            kullanici.BasarisizGirisSayisi++;
            if (kullanici.BasarisizGirisSayisi >= MaksimumHataliGiris)
            {
                kullanici.KilitBitisZamani = DateTime.Now.Add(KilitSuresi);
                kullanici.BasarisizGirisSayisi = 0;
            }
            await _repository.SaveAsync();
            return new GirisSonucu(false, null, GenelHata);
        }

        if (!kullanici.AktifMi)
            return new GirisSonucu(false, null, "Hesap devre dışı.");

        kullanici.BasarisizGirisSayisi = 0;
        kullanici.KilitBitisZamani = null;
        kullanici.SonGirisZamani = DateTime.Now;
        await _repository.SaveAsync();
        return new GirisSonucu(true, kullanici, null);
    }

    public Task<List<Kullanici>> TumKullanicilarAsync() => _repository.GetTumuDetayliAsync();

    public async Task KilidiKaldirAsync(int kullaniciId)
    {
        var kullanici = await _repository.GetByIdAsync(kullaniciId)
            ?? throw new KeyNotFoundException($"Kullanıcı bulunamadı (Id={kullaniciId}).");
        kullanici.BasarisizGirisSayisi = 0;
        kullanici.KilitBitisZamani = null;
        await _repository.SaveAsync();
    }

    // En az 8 karakter, en az bir harf ve bir rakam.
    public static void ParolaKuraliniKontrolEt(string parola)
    {
        if (parola.Length < 8 || !parola.Any(char.IsLetter) || !parola.Any(char.IsDigit))
            throw new ArgumentException("Parola en az 8 karakter olmalı ve harf ile rakam içermeli.");
    }
}
