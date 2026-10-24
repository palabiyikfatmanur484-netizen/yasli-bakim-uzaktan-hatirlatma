using System.Net;
using YasliBakim.DataAccess.Seed;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Tests;

/// <summary>
/// Gerçek HTTP istekleriyle kimlik doğrulama ve rol tabanlı erişim testleri.
/// </summary>
public class YetkiTestleri : IClassFixture<UygulamaFabrikasi>
{
    private const string Parola = OrnekVeri.DemoParola;
    private readonly UygulamaFabrikasi _fabrika;

    public YetkiTestleri(UygulamaFabrikasi fabrika)
    {
        _fabrika = fabrika;
    }

    private async Task<HttpClient> GirisliIstemciAsync(string eposta)
    {
        var istemci = _fabrika.IstemciOlustur();
        var yanit = await _fabrika.GirisYapAsync(istemci, eposta, Parola);
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        return istemci;
    }

    private static void ErisimEngellendiMi(HttpResponseMessage yanit)
    {
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Panelim")]
    [InlineData("/Yonetim/Kullanicilar")]
    [InlineData("/Yaslilar/Detay/1")]
    public async Task GirisYapmamisKullanici_GirisSayfasinaYonlendirilir(string adres)
    {
        var yanit = await _fabrika.IstemciOlustur().GetAsync(adres);

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/Giris", yanit.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task GirisSayfasi_HerkeseAcik()
    {
        var yanit = await _fabrika.IstemciOlustur().GetAsync("/Hesap/Giris");
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    [Fact]
    public async Task YanlisParola_OturumAcmaz()
    {
        var istemci = _fabrika.IstemciOlustur();
        var yanit = await _fabrika.GirisYapAsync(istemci, "selin@ornek.test", "Yanlis123");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        // Razor Türkçe karakterleri HTML olarak kodlar ("hatal&#x131;"), bu yüzden metnin ilk kısmına bakılır.
        Assert.Contains("E-posta veya parola hatal", await yanit.Content.ReadAsStringAsync());
        Assert.False(yanit.Headers.Contains("Set-Cookie") &&
                     yanit.Headers.GetValues("Set-Cookie").Any(c => c.StartsWith("YasliBakim.Oturum")));
    }

    [Fact]
    public async Task OturumCerezi_HttpOnlyVeSecure()
    {
        var yanit = await _fabrika.GirisYapAsync(_fabrika.IstemciOlustur(), "selin@ornek.test", Parola);

        var cerez = yanit.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("YasliBakim.Oturum"));
        Assert.Contains("httponly", cerez, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cerez, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cerez, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Bakici_YalnizcaKendiYaslilariniGorur()
    {
        var istemci = await GirisliIstemciAsync("selin@ornek.test");   // Ayşe, Fatma, Zeynep'e atanmış

        var html = await istemci.GetStringAsync("/");

        Assert.Contains("Fatma", html);
        Assert.DoesNotContain("Hasan", html);
        Assert.DoesNotContain("Mehmet", html);
    }

    [Fact]
    public async Task Bakici_AtanmamisYaslininDetayinaGiremez()
    {
        var istemci = await GirisliIstemciAsync("selin@ornek.test");
        var hasanId = _fabrika.Sorgula(c => c.Yaslilar.Single(y => y.Ad == "Hasan").Id);
        var fatmaId = _fabrika.Sorgula(c => c.Yaslilar.Single(y => y.Ad == "Fatma").Id);

        ErisimEngellendiMi(await istemci.GetAsync($"/Yaslilar/Detay/{hasanId}"));
        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync($"/Yaslilar/Detay/{fatmaId}")).StatusCode);
    }

    [Fact]
    public async Task Bakici_YonetimSayfasinaGiremez()
    {
        var istemci = await GirisliIstemciAsync("selin@ornek.test");
        ErisimEngellendiMi(await istemci.GetAsync("/Yonetim/Kullanicilar"));
    }

    [Fact]
    public async Task Yonetici_YonetimSayfasinaVeTumYaslilaraErisir()
    {
        var istemci = await GirisliIstemciAsync("yonetici@ornek.test");

        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/Yonetim/Kullanicilar")).StatusCode);
        var html = await istemci.GetStringAsync("/");
        Assert.Contains("Hasan", html);
        Assert.Contains("Fatma", html);
    }

    [Fact]
    public async Task Yasli_KendiPaneliniGorur_DigerSayfalaraGiremez()
    {
        var istemci = await GirisliIstemciAsync("ayse@ornek.test");

        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/Panelim")).StatusCode);
        ErisimEngellendiMi(await istemci.GetAsync("/"));
        ErisimEngellendiMi(await istemci.GetAsync("/Yonetim/Kullanicilar"));
    }

    [Fact]
    public async Task Cikis_SonrasiKorumaliSayfaTekrarGirisIster()
    {
        var istemci = await GirisliIstemciAsync("selin@ornek.test");
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, "/");

        await istemci.PostAsync("/Hesap/Cikis", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        }));

        var yanit = await istemci.GetAsync("/");
        Assert.Contains("/Hesap/Giris", yanit.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Kayit_FormaRolEklenseBileBakiciOlarakAcilir()
    {
        var istemci = _fabrika.IstemciOlustur();
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, "/Hesap/Kayit");

        var yanit = await istemci.PostAsync("/Hesap/Kayit", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Ad"] = "Saldırgan", ["Soyad"] = "Deneme", ["Eposta"] = "saldirgan@ornek.test",
            ["Telefon"] = "0500 000 00 09", ["Parola"] = "Parola123", ["ParolaTekrar"] = "Parola123",
            ["Rol"] = "Yonetici",                                   // yetki yükseltme denemesi
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        var rol = _fabrika.Sorgula(c => c.Kullanicilar.Single(k => k.Eposta == "saldirgan@ornek.test").Rol);
        Assert.Equal(KullaniciRolu.Bakici, rol);
        ErisimEngellendiMi(await istemci.GetAsync("/Yonetim/Kullanicilar"));
    }

    [Fact]
    public async Task AntiForgeryTokenOlmadanPostReddedilir()
    {
        var istemci = _fabrika.IstemciOlustur();
        var yanit = await istemci.PostAsync("/Hesap/Giris", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Eposta"] = "selin@ornek.test", ["Parola"] = Parola,
        }));

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
    }
}
