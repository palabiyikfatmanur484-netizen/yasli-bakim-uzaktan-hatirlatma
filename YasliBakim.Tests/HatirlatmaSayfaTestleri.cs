using System.Net;
using Microsoft.Extensions.DependencyInjection;
using YasliBakim.DataAccess.Context;
using YasliBakim.DataAccess.Seed;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Tests;

/// <summary>
/// Hatırlatma sayfalarının gerçek HTTP istekleriyle testi: sahiplik kontrolü ve onay akışı.
/// </summary>
public class HatirlatmaSayfaTestleri : IClassFixture<UygulamaFabrikasi>
{
    private readonly UygulamaFabrikasi _fabrika;

    public HatirlatmaSayfaTestleri(UygulamaFabrikasi fabrika)
    {
        _fabrika = fabrika;
    }

    private int YasliId(string ad) => _fabrika.Sorgula(c => c.Yaslilar.Single(y => y.Ad == ad).Id);

    private async Task<HttpClient> GirisliAsync(string eposta)
    {
        var istemci = _fabrika.IstemciOlustur();
        await _fabrika.GirisYapAsync(istemci, eposta, OrnekVeri.DemoParola);
        return istemci;
    }

    private static void Engellendi(HttpResponseMessage yanit) =>
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location?.OriginalString ?? "");

    // Saati "şimdi" olan, onay penceresi açık bir hatırlatmayı doğrudan veritabanına ekler.
    private Hatirlatma SimdikiHatirlatmaEkle(string yasliAdi, string baslik)
    {
        using var scope = _fabrika.Services.CreateScope();
        var c = scope.ServiceProvider.GetRequiredService<YasliBakimDbContext>();
        var simdi = DateTime.Now;
        var h = new Hatirlatma
        {
            YasliId = c.Yaslilar.Single(y => y.Ad == yasliAdi).Id, Baslik = baslik, Tur = HatirlatmaTuru.Ilac,
            Saat = new TimeOnly(simdi.Hour, simdi.Minute), Tekrar = TekrarTipi.Gunluk,
            BaslangicTarihi = DateOnly.FromDateTime(simdi), OlusturmaTarihi = simdi.AddHours(-1),
        };
        c.Hatirlatmalar.Add(h);
        c.SaveChanges();
        return h;
    }

    [Fact]
    public async Task Bakici_KendiYaslisinaHatirlatmaEkleyebilir()
    {
        var istemci = await GirisliAsync("selin@ornek.test");
        var fatmaId = YasliId("Fatma");
        var adres = $"/Hatirlatmalar/Duzenle?yasliId={fatmaId}";
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, adres);

        var yanit = await istemci.PostAsync(adres, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Form.Baslik"] = "Kalsiyum", ["Form.Tur"] = "1", ["Form.Saat"] = "14:00", ["Form.Tekrar"] = "2",
            ["Form.BaslangicTarihi"] = DateTime.Today.ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.True(_fabrika.Sorgula(c => c.Hatirlatmalar.Any(h => h.YasliId == fatmaId && h.Baslik == "Kalsiyum")));
    }

    [Fact]
    public async Task Bakici_AtanmamisYasliyaHatirlatmaEkleyemezVeDuzenleyemez()
    {
        var istemci = await GirisliAsync("selin@ornek.test");
        var hasanId = YasliId("Hasan");
        var hasaninHatirlatmasi = _fabrika.Sorgula(c => c.Hatirlatmalar.First(h => h.YasliId == hasanId).Id);

        Engellendi(await istemci.GetAsync($"/Hatirlatmalar/Duzenle?yasliId={hasanId}"));
        Engellendi(await istemci.GetAsync($"/Hatirlatmalar/Duzenle/{hasaninHatirlatmasi}"));
        // Kendi yaşlısının id'si adres çubuğuna yazılsa bile düzenlemede yaşlı kayıttan alınır.
        Engellendi(await istemci.GetAsync($"/Hatirlatmalar/Duzenle/{hasaninHatirlatmasi}?yasliId={YasliId("Fatma")}"));
    }

    [Fact]
    public async Task Form_IsKuraliHatasiniGosterir()
    {
        var istemci = await GirisliAsync("selin@ornek.test");
        var adres = $"/Hatirlatmalar/Duzenle?yasliId={YasliId("Zeynep")}";
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, adres);

        var yanit = await istemci.PostAsync(adres, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Form.Baslik"] = "Geçmiş", ["Form.Tur"] = "1", ["Form.Saat"] = "10:00", ["Form.Tekrar"] = "2",
            ["Form.BaslangicTarihi"] = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);      // kaydedilmedi, form tekrar gösterildi
        Assert.Contains("alert-danger", await yanit.Content.ReadAsStringAsync());
        Assert.False(_fabrika.Sorgula(c => c.Hatirlatmalar.Any(h => h.Baslik == "Geçmiş")));
    }

    [Fact]
    public async Task Yasli_PaneldenHatirlatmayiOnaylar()
    {
        var h = SimdikiHatirlatmaEkle("Ayşe", "Panel testi");
        var istemci = await GirisliAsync("ayse@ornek.test");
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, "/Panelim");
        var planlanan = DateOnly.FromDateTime(DateTime.Now).ToDateTime(h.Saat);

        var yanit = await istemci.PostAsync("/Panelim?handler=Yanit", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["hatirlatmaId"] = h.Id.ToString(), ["planlanan"] = planlanan.ToString("yyyy-MM-ddTHH:mm:ss"), ["alindi"] = "true",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal(AktiviteDurumu.Tamamlandi,
            _fabrika.Sorgula(c => c.AktiviteKayitlari.Single(a => a.HatirlatmaId == h.Id).Durum));
    }

    [Fact]
    public async Task Yasli_BaskasininHatirlatmasiniOnaylayamaz()
    {
        var h = SimdikiHatirlatmaEkle("Ayşe", "Başkasının ilacı");
        var istemci = await GirisliAsync("hasan@ornek.test");
        var token = await UygulamaFabrikasi.TokenAlAsync(istemci, "/Panelim");
        var planlanan = DateOnly.FromDateTime(DateTime.Now).ToDateTime(h.Saat);

        var yanit = await istemci.PostAsync("/Panelim?handler=Yanit", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["hatirlatmaId"] = h.Id.ToString(), ["planlanan"] = planlanan.ToString("yyyy-MM-ddTHH:mm:ss"), ["alindi"] = "true",
            ["__RequestVerificationToken"] = token,
        }));

        Engellendi(yanit);
        Assert.False(_fabrika.Sorgula(c => c.AktiviteKayitlari.Any(a => a.HatirlatmaId == h.Id)));
    }
}
