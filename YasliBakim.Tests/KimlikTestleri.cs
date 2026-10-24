using Microsoft.EntityFrameworkCore;
using YasliBakim.Business.Concrete;
using YasliBakim.Business.Guvenlik;
using YasliBakim.DataAccess.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Tests;

public class KimlikTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    // Testler hızlı çalışsın diye düşük iterasyon; uygulamada 600.000 kullanılır.
    private readonly Pbkdf2ParolaHasher _hasher = new(iterasyon: 1_000);
    private readonly KimlikManager _kimlik;

    public KimlikTestleri()
    {
        _kimlik = new KimlikManager(new EfKullaniciRepository(_db.Context), new EfBakiciRepository(_db.Context), _hasher);
    }

    public void Dispose() => _db.Dispose();

    // --- Parola hash ---

    [Fact]
    public void Hash_DuzMetinIcermez_VeDogrulanir()
    {
        var hash = _hasher.Hashle("Gizli1234");

        Assert.DoesNotContain("Gizli1234", hash);
        Assert.StartsWith("PBKDF2$", hash);
        Assert.True(_hasher.Dogrula("Gizli1234", hash));
        Assert.False(_hasher.Dogrula("gizli1234", hash));
    }

    [Fact]
    public void AyniParola_FarkliTuzlaFarkliHashUretir()
    {
        Assert.NotEqual(_hasher.Hashle("Gizli1234"), _hasher.Hashle("Gizli1234"));
    }

    [Fact]
    public void BozukHash_DogrulanmazVeHataFirlatmaz()
    {
        Assert.False(_hasher.Dogrula("Gizli1234", "duz-metin-parola"));
    }

    // --- Kayıt ---

    [Fact]
    public async Task Kayit_ParolayiHashliSaklar_VeRoluBakiciYapar()
    {
        var k = await _kimlik.BakiciKayitAsync("Deniz", "Örnek", " Deniz@Ornek.Test ", "0500 000 00 01", "Parola123");

        _db.Context.ChangeTracker.Clear();
        var kayitli = await _db.Context.Kullanicilar.Include(x => x.Bakici).SingleAsync();
        Assert.Equal("deniz@ornek.test", kayitli.Eposta);
        Assert.NotEqual("Parola123", kayitli.ParolaHash);
        Assert.Equal(KullaniciRolu.Bakici, kayitli.Rol);
        Assert.NotNull(kayitli.Bakici);
        Assert.Equal(k.Id, kayitli.Id);
    }

    [Theory]
    [InlineData("kisa1")]          // 8 karakterden kısa
    [InlineData("sadeceharf")]     // rakam yok
    [InlineData("12345678")]       // harf yok
    public async Task Kayit_ZayifParolayiReddeder(string parola)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _kimlik.BakiciKayitAsync("A", "B", "a@ornek.test", "0500 000 00 01", parola));
    }

    [Fact]
    public async Task Kayit_AyniEpostaIkinciKezAcilamaz()
    {
        await _kimlik.BakiciKayitAsync("A", "B", "a@ornek.test", "0500 000 00 01", "Parola123");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kimlik.BakiciKayitAsync("C", "D", "A@ornek.test", "0500 000 00 02", "Parola123"));
    }

    // --- Giriş ---

    [Fact]
    public async Task Giris_DogruParolaIleBasarili()
    {
        await _kimlik.BakiciKayitAsync("A", "B", "a@ornek.test", "0500 000 00 01", "Parola123");

        var sonuc = await _kimlik.GirisAsync("a@ornek.test", "Parola123");

        Assert.True(sonuc.Basarili);
        Assert.NotNull(sonuc.Kullanici!.SonGirisZamani);
    }

    [Fact]
    public async Task Giris_YanlisParolaVeOlmayanKullaniciAyniMesajiVerir()
    {
        await _kimlik.BakiciKayitAsync("A", "B", "a@ornek.test", "0500 000 00 01", "Parola123");

        var yanlisParola = await _kimlik.GirisAsync("a@ornek.test", "Yanlis123");
        var olmayan = await _kimlik.GirisAsync("yok@ornek.test", "Yanlis123");

        Assert.False(yanlisParola.Basarili);
        Assert.False(olmayan.Basarili);
        Assert.Equal(yanlisParola.Hata, olmayan.Hata);
    }

    [Fact]
    public async Task Giris_BesHataliDenemedeHesapKilitlenir_DogruParolaDaReddedilir()
    {
        await _kimlik.BakiciKayitAsync("A", "B", "a@ornek.test", "0500 000 00 01", "Parola123");

        for (var i = 0; i < KimlikManager.MaksimumHataliGiris; i++)
            await _kimlik.GirisAsync("a@ornek.test", "Yanlis123");

        var sonuc = await _kimlik.GirisAsync("a@ornek.test", "Parola123");
        Assert.False(sonuc.Basarili);
        Assert.Contains("kilitli", sonuc.Hata);

        // Yönetici kilidi kaldırınca tekrar girilebilir.
        var kullanici = await _db.Context.Kullanicilar.SingleAsync();
        await _kimlik.KilidiKaldirAsync(kullanici.Id);
        Assert.True((await _kimlik.GirisAsync("a@ornek.test", "Parola123")).Basarili);
    }

    [Fact]
    public async Task Giris_BasariliGirisHataSayaciniSifirlar()
    {
        await _kimlik.BakiciKayitAsync("A", "B", "a@ornek.test", "0500 000 00 01", "Parola123");
        await _kimlik.GirisAsync("a@ornek.test", "Yanlis123");
        await _kimlik.GirisAsync("a@ornek.test", "Yanlis123");

        await _kimlik.GirisAsync("a@ornek.test", "Parola123");

        Assert.Equal(0, (await _db.Context.Kullanicilar.SingleAsync()).BasarisizGirisSayisi);
    }

    [Fact]
    public async Task CheckKisiti_YasliRolundeYasliIdZorunlu()
    {
        _db.Context.Kullanicilar.Add(new() { Eposta = "x@ornek.test", ParolaHash = "x", Rol = KullaniciRolu.Yasli });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }
}
