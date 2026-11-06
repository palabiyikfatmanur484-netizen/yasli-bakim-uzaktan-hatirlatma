using System.Net;
using YasliBakim.DataAccess.Seed;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Tests;

/// <summary>
/// Günlük durum ve aktivite sayfalarının gerçek HTTP istekleriyle testi: giriş, sahiplik ve rol kısıtları.
/// </summary>
public class AktiviteSayfaTestleri : IClassFixture<UygulamaFabrikasi>
{
    private readonly UygulamaFabrikasi _fabrika;

    public AktiviteSayfaTestleri(UygulamaFabrikasi fabrika)
    {
        _fabrika = fabrika;
    }

    private int YasliId(string ad) => _fabrika.Sorgula(c => c.Yaslilar.Single(y => y.Ad == ad).Id);
    private int BakiciId(string eposta) => _fabrika.Sorgula(c => c.Bakicilar.Single(b => b.Eposta == eposta).Id);

    private async Task<HttpClient> GirisliAsync(string eposta)
    {
        var istemci = _fabrika.IstemciOlustur();
        await _fabrika.GirisYapAsync(istemci, eposta, OrnekVeri.DemoParola);
        return istemci;
    }

    // Razor Türkçe karakterleri HTML varlığı olarak yazar (ş → &#x15F;); karşılaştırmadan önce çözülür.
    private static async Task<string> SayfaMetniAsync(HttpClient istemci, string adres) =>
        WebUtility.HtmlDecode(await istemci.GetStringAsync(adres));

    private static void Engellendi(HttpResponseMessage yanit) =>
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location?.OriginalString ?? "");

    private static async Task<HttpResponseMessage> AktiviteGonderAsync(HttpClient istemci, string adres, string not,
        string kategori = "Hobi")
    {
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, adres);
        return await istemci.PostAsync(adres, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Form.Kategori"] = kategori, ["Form.SureDakika"] = "30",
            ["Form.Zaman"] = DateTime.Now.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm"), ["Form.Not"] = not,
            ["__RequestVerificationToken"] = token,
        }));
    }

    [Fact]
    public async Task Yasli_PaneldenGunlukDurumBildirir_KotuDurumOlayUretir()
    {
        var istemci = await GirisliAsync("hasan@ornek.test");
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, "/Panelim");

        var yanit = await istemci.PostAsync("/Panelim?handler=Durum", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["ruhHali"] = "Kotu", ["agri"] = "8", ["not"] = "Dizim ağrıyor", ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        var hasanId = YasliId("Hasan");
        var bugun = DateTime.Today;
        var kayit = _fabrika.Sorgula(c => c.AktiviteKayitlari.Single(a =>
            a.YasliId == hasanId && a.Tur == AktiviteTuru.GunlukDurum && a.KayitZamani >= bugun));
        Assert.Equal((RuhHali.Kotu, 8), (kayit.RuhHali, kayit.AgriSeviyesi));
        Assert.True(_fabrika.Sorgula(c => c.Olaylar.Any(o => o.AktiviteKaydiId == kayit.Id && o.Tur == OlayTuru.Anomali)));
        Assert.Contains("Dizim ağrıyor", await SayfaMetniAsync(istemci, "/Panelim"));
    }

    [Fact]
    public async Task Yasli_AdresteBaskaYasliIdOlsaBileKayitKendisineYazilir()
    {
        var istemci = await GirisliAsync("ayse@ornek.test");

        var yanit = await AktiviteGonderAsync(istemci, $"/Aktiviteler/Yeni?yasliId={YasliId("Hasan")}", "Ayşe'nin örgüsü");

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        var kayit = _fabrika.Sorgula(c => c.AktiviteKayitlari.Single(a => a.Not == "Ayşe'nin örgüsü"));
        Assert.Equal(YasliId("Ayşe"), kayit.YasliId);
        Assert.Null(kayit.KaydedenBakiciId);
    }

    [Fact]
    public async Task Bakici_AtanmisYasliyaAktiviteGirer_KaydedenOlarakGorunur()
    {
        var istemci = await GirisliAsync("selin@ornek.test");
        var fatmaId = YasliId("Fatma");

        var yanit = await AktiviteGonderAsync(istemci, $"/Aktiviteler/Yeni?yasliId={fatmaId}", "Birlikte yürüdük", "Yuruyus");

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        var kayit = _fabrika.Sorgula(c => c.AktiviteKayitlari.Single(a => a.Not == "Birlikte yürüdük"));
        Assert.Equal((fatmaId, BakiciId("selin@ornek.test")), (kayit.YasliId, kayit.KaydedenBakiciId));
        Assert.Contains("Kaydeden: Selin", await SayfaMetniAsync(istemci, $"/Aktiviteler/Gecmis?yasliId={fatmaId}"));
    }

    [Fact]
    public async Task Bakici_AtanmamisYaslininGecmisiniGoremezKayitGiremez()
    {
        var istemci = await GirisliAsync("selin@ornek.test");
        var hasanId = YasliId("Hasan");

        Engellendi(await istemci.GetAsync($"/Aktiviteler/Gecmis?yasliId={hasanId}"));
        Engellendi(await istemci.GetAsync($"/Aktiviteler/Yeni?yasliId={hasanId}"));
    }

    [Fact]
    public async Task Yasli_GecmisteYalnizcaKendiVerisiniGorur()
    {
        var istemci = await GirisliAsync("hasan@ornek.test");

        var html = await SayfaMetniAsync(istemci, $"/Aktiviteler/Gecmis?yasliId={YasliId("Ayşe")}");

        Assert.Contains("Geçmişim", html);
        Assert.DoesNotContain("Kızımla telefonda konuştum", html);      // Ayşe'nin örnek kaydı
    }

    [Fact]
    public async Task Form_IsKuraliHatasiniGosterir()
    {
        var istemci = await GirisliAsync("ayse@ornek.test");
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, "/Aktiviteler/Yeni");

        var yanit = await istemci.PostAsync("/Aktiviteler/Yeni", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Form.Kategori"] = "Yuruyus", ["Form.SureDakika"] = "30",
            ["Form.Zaman"] = DateTime.Now.AddDays(-5).ToString("yyyy-MM-ddTHH:mm"), ["Form.Not"] = "Eski yürüyüş",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);      // kaydedilmedi, form tekrar gösterildi
        Assert.Contains("alert-danger", await yanit.Content.ReadAsStringAsync());
        Assert.False(_fabrika.Sorgula(c => c.AktiviteKayitlari.Any(a => a.Not == "Eski yürüyüş")));
    }

    [Fact]
    public async Task Yasli_KendiGirdigiAktiviteyiSiler_BakiciSilemez()
    {
        var yasli = await GirisliAsync("ayse@ornek.test");
        await AktiviteGonderAsync(yasli, "/Aktiviteler/Yeni", "Silinecek kayıt", "EvIsi");
        var kayitId = _fabrika.Sorgula(c => c.AktiviteKayitlari.Single(a => a.Not == "Silinecek kayıt").Id);
        var ayseId = YasliId("Ayşe");

        // Ayşe'ye atanmış bakıcı bile yaşlının girdiği kaydı silemez.
        var bakici = await GirisliAsync("selin@ornek.test");
        var bakiciToken = await UygulamaFabrikasi.TokenAlAsync(bakici, $"/Aktiviteler/Gecmis?yasliId={ayseId}");
        Engellendi(await bakici.PostAsync($"/Aktiviteler/Gecmis?handler=Sil&yasliId={ayseId}", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["kayitId"] = kayitId.ToString(), ["__RequestVerificationToken"] = bakiciToken })));
        Assert.True(_fabrika.Sorgula(c => c.AktiviteKayitlari.Any(a => a.Id == kayitId)));

        var token = await UygulamaFabrikasi.TokenAlAsync(yasli, "/Aktiviteler/Gecmis");
        var yanit = await yasli.PostAsync("/Aktiviteler/Gecmis?handler=Sil", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["kayitId"] = kayitId.ToString(), ["__RequestVerificationToken"] = token }));

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.False(_fabrika.Sorgula(c => c.AktiviteKayitlari.Any(a => a.Id == kayitId)));
    }

    [Fact]
    public async Task Yonetici_GecmisiGorurAmaKayitGiremezVeSilemez()
    {
        var istemci = await GirisliAsync("yonetici@ornek.test");
        var ayseId = YasliId("Ayşe");
        var kayitId = _fabrika.Sorgula(c => c.AktiviteKayitlari.First(a => a.YasliId == ayseId && a.Tur == AktiviteTuru.Aktivite).Id);

        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync($"/Aktiviteler/Gecmis?yasliId={ayseId}")).StatusCode);
        Engellendi(await istemci.GetAsync($"/Aktiviteler/Yeni?yasliId={ayseId}"));

        // Yöneticinin bakıcı ve yaşlı Id'si yok; "yaşlının girdiği kayıt" gibi görünüp silebilmesi engellenmeli.
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, $"/Aktiviteler/Gecmis?yasliId={ayseId}");
        Engellendi(await istemci.PostAsync($"/Aktiviteler/Gecmis?handler=Sil&yasliId={ayseId}", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["kayitId"] = kayitId.ToString(), ["__RequestVerificationToken"] = token })));
        Assert.True(_fabrika.Sorgula(c => c.AktiviteKayitlari.Any(a => a.Id == kayitId)));
    }
}
