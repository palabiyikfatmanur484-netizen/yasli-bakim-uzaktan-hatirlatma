using Microsoft.EntityFrameworkCore;
using YasliBakim.Business.Concrete;
using YasliBakim.DataAccess.Concrete;
using YasliBakim.DataAccess.Seed;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Tests;

public class CrudTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private readonly YasliManager _yasliService;
    private readonly BakiciManager _bakiciService;
    private readonly HatirlatmaManager _hatirlatmaService;
    private readonly AktiviteManager _aktiviteService;
    private readonly OlayManager _olayService;

    public CrudTestleri()
    {
        var c = _db.Context;
        var yasliRepo = new EfYasliRepository(c);
        var hatirlatmaRepo = new EfHatirlatmaRepository(c);
        var aktiviteRepo = new EfAktiviteKaydiRepository(c);
        var olayRepo = new EfOlayRepository(c);

        _yasliService = new YasliManager(yasliRepo);
        _bakiciService = new BakiciManager(new EfBakiciRepository(c), new EfGenericRepository<YasliBakici>(c), yasliRepo);
        _hatirlatmaService = new HatirlatmaManager(hatirlatmaRepo, aktiviteRepo);
        _aktiviteService = new AktiviteManager(aktiviteRepo, hatirlatmaRepo, olayRepo);
        _olayService = new OlayManager(olayRepo);
    }

    public void Dispose() => _db.Dispose();

    private static Yasli YeniYasli(string ad = "Test") =>
        new() { Ad = ad, Soyad = "Kişi", DogumTarihi = new DateOnly(1945, 1, 1) };

    private static Bakici YeniBakici(string eposta = "test@ornek.test") =>
        new() { Ad = "Test", Soyad = "Bakıcı", Eposta = eposta, Telefon = "0500 000 00 00", Rol = BakiciRolu.AileYakini };

    private static Hatirlatma YeniHatirlatma(int yasliId) =>
        new()
        {
            YasliId = yasliId, Baslik = "İlaç", Tur = HatirlatmaTuru.Ilac, Saat = new TimeOnly(9, 0),
            Tekrar = TekrarTipi.Gunluk, BaslangicTarihi = DateOnly.FromDateTime(DateTime.Today).AddDays(-1),
        };

    // --- CRUD ---

    [Fact]
    public async Task Yasli_Ekle_Getir_Guncelle_Sil()
    {
        var yasli = YeniYasli();
        await _yasliService.EkleAsync(yasli);
        Assert.True(yasli.Id > 0);

        yasli.Telefon = "0500 000 99 99";
        await _yasliService.GuncelleAsync(yasli);
        _db.Context.ChangeTracker.Clear();
        Assert.Equal("0500 000 99 99", (await _yasliService.GetirAsync(yasli.Id))!.Telefon);

        await _yasliService.SilAsync(yasli.Id);
        Assert.Null(await _yasliService.GetirAsync(yasli.Id));
    }

    [Fact]
    public async Task Yasli_GelecekDogumTarihi_Reddedilir()
    {
        var yasli = YeniYasli();
        yasli.DogumTarihi = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        await Assert.ThrowsAsync<ArgumentException>(() => _yasliService.EkleAsync(yasli));
    }

    [Fact]
    public async Task Hatirlatma_BugunkuListe_PasifVeBitmisleriHaricTutar()
    {
        var yasli = YeniYasli();
        await _yasliService.EkleAsync(yasli);
        var bugun = DateOnly.FromDateTime(DateTime.Today);

        var gecerli = YeniHatirlatma(yasli.Id);
        var pasif = YeniHatirlatma(yasli.Id); pasif.AktifMi = false;
        var bitmis = YeniHatirlatma(yasli.Id); bitmis.BaslangicTarihi = bugun.AddDays(-10); bitmis.BitisTarihi = bugun.AddDays(-1);
        var gelecek = YeniHatirlatma(yasli.Id); gelecek.BaslangicTarihi = bugun.AddDays(3);
        foreach (var h in new[] { gecerli, pasif, bitmis, gelecek })
            await _hatirlatmaService.EkleAsync(h);

        var liste = await _hatirlatmaService.BugunkuHatirlatmalarAsync(yasli.Id);
        Assert.Equal(gecerli.Id, Assert.Single(liste).Id);
    }

    // --- N-N ilişki ---

    [Fact]
    public async Task Bakici_YasliyaAtanir_VeTekrarAtanamaz()
    {
        var y1 = YeniYasli("A"); var y2 = YeniYasli("B");
        await _yasliService.EkleAsync(y1);
        await _yasliService.EkleAsync(y2);
        var bakici = YeniBakici();
        await _bakiciService.EkleAsync(bakici);

        await _bakiciService.YasliyaAtaAsync(y1.Id, bakici.Id, "Kızı", true);
        await _bakiciService.YasliyaAtaAsync(y2.Id, bakici.Id, null, false);

        Assert.Equal(2, (await _bakiciService.YaslilariGetirAsync(bakici.Id)).Count);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _bakiciService.YasliyaAtaAsync(y1.Id, bakici.Id, null, false));

        await _bakiciService.AtamayiKaldirAsync(y1.Id, bakici.Id);
        Assert.Single(await _bakiciService.YaslilariGetirAsync(bakici.Id));
    }

    // --- Kısıtlar (servis atlanarak doğrudan veritabanına yazılır) ---

    [Fact]
    public async Task UniqueIndeks_AyniEpostaIkinciKezEklenemez()
    {
        _db.Context.Bakicilar.Add(YeniBakici("ayni@ornek.test"));
        _db.Context.Bakicilar.Add(YeniBakici("ayni@ornek.test"));
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task YabanciAnahtar_OlmayanYasliyaHatirlatmaEklenemez()
    {
        _db.Context.Hatirlatmalar.Add(YeniHatirlatma(yasliId: 999));
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task CheckKisiti_BitisTarihiBaslangictanOnceOlamaz()
    {
        var yasli = YeniYasli();
        await _yasliService.EkleAsync(yasli);
        var h = YeniHatirlatma(yasli.Id);
        h.BitisTarihi = h.BaslangicTarihi.AddDays(-1);

        _db.Context.Hatirlatmalar.Add(h);
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task CheckKisiti_GunlukDurumdaRuhHaliZorunlu()
    {
        var yasli = YeniYasli();
        await _yasliService.EkleAsync(yasli);

        _db.Context.AktiviteKayitlari.Add(new AktiviteKaydi { YasliId = yasli.Id, Tur = AktiviteTuru.GunlukDurum });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    // --- Silme davranışları ---

    [Fact]
    public async Task YasliSilinince_BagliKayitlarCascadeIleSilinir()
    {
        var yasli = YeniYasli();
        await _yasliService.EkleAsync(yasli);
        var h = YeniHatirlatma(yasli.Id);
        await _hatirlatmaService.EkleAsync(h);
        await _aktiviteService.HatirlatmaOnaylaAsync(h.Id, AktiviteDurumu.Atlandi);
        _db.Context.ChangeTracker.Clear();

        await _yasliService.SilAsync(yasli.Id);

        Assert.Equal(0, await _db.Context.Hatirlatmalar.CountAsync());
        Assert.Equal(0, await _db.Context.AktiviteKayitlari.CountAsync());
        Assert.Equal(0, await _db.Context.Olaylar.CountAsync());
    }

    [Fact]
    public async Task BakiciSilinince_HatirlatmadakiAlanNullOlur()
    {
        var yasli = YeniYasli();
        await _yasliService.EkleAsync(yasli);
        var bakici = YeniBakici();
        await _bakiciService.EkleAsync(bakici);
        var h = YeniHatirlatma(yasli.Id);
        h.OlusturanBakiciId = bakici.Id;
        await _hatirlatmaService.EkleAsync(h);
        _db.Context.ChangeTracker.Clear();

        await _bakiciService.SilAsync(bakici.Id);
        _db.Context.ChangeTracker.Clear();

        Assert.Null((await _hatirlatmaService.GetirAsync(h.Id))!.OlusturanBakiciId);
    }

    [Fact]
    public async Task GecmisiOlanHatirlatmaSilinemez_PasifeAlinir()
    {
        var yasli = YeniYasli();
        await _yasliService.EkleAsync(yasli);
        var h = YeniHatirlatma(yasli.Id);
        await _hatirlatmaService.EkleAsync(h);
        await _aktiviteService.HatirlatmaOnaylaAsync(h.Id, AktiviteDurumu.Tamamlandi);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _hatirlatmaService.SilAsync(h.Id));
        await _hatirlatmaService.PasifeAlAsync(h.Id);
        Assert.False((await _hatirlatmaService.GetirAsync(h.Id))!.AktifMi);
    }

    // --- İş akışı ---

    [Fact]
    public async Task AtlananIlac_OlayOlusturur_VeOlayCozulur()
    {
        var yasli = YeniYasli();
        await _yasliService.EkleAsync(yasli);
        var h = YeniHatirlatma(yasli.Id);
        await _hatirlatmaService.EkleAsync(h);

        var kayit = await _aktiviteService.HatirlatmaOnaylaAsync(h.Id, AktiviteDurumu.Atlandi);

        var olay = Assert.Single(await _olayService.AcikOlaylarAsync());
        Assert.Equal(kayit.Id, olay.AktiviteKaydiId);
        Assert.Equal(OlaySeviyesi.Yuksek, olay.Seviye);

        await _olayService.CozulduIsaretleAsync(olay.Id);
        Assert.Empty(await _olayService.AcikOlaylarAsync());
    }

    // --- Örnek veri ---

    [Fact]
    public async Task OrnekVeri_YuklenirVeSentetiktir()
    {
        await OrnekVeri.YukleAsync(_db.Context);
        await OrnekVeri.YukleAsync(_db.Context); // ikinci çağrı tekrar eklememeli

        Assert.Equal(5, await _db.Context.Yaslilar.CountAsync());
        Assert.True(await _db.Context.YasliBakicilar.CountAsync() >= 5);
        // KVKK: gerçek iletişim bilgisi yok.
        Assert.All(await _db.Context.Bakicilar.ToListAsync(), b => Assert.EndsWith("@ornek.test", b.Eposta));
        Assert.All(await _db.Context.Yaslilar.ToListAsync(), y => Assert.StartsWith("0500 000", y.Telefon));
    }
}
