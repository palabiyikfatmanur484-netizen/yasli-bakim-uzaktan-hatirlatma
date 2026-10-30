using Microsoft.EntityFrameworkCore;
using YasliBakim.Business.Abstract;
using YasliBakim.Business.Concrete;
using YasliBakim.DataAccess.Concrete;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Tests;

// Hatırlatma modülü: doğrulama, onay/geri bildirim ve kaçırma iş kuralları.
public class HatirlatmaTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private static readonly DateOnly Bugun = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateTime Bugun0800 = Bugun.ToDateTime(new TimeOnly(8, 0));
    private readonly SabitSaat _saat = new(Bugun0800.AddMinutes(-90));   // 06:30, onay henüz açık değil
    private readonly HatirlatmaManager _hatirlatma;
    private readonly HatirlatmaTakipManager _takip;
    private int _yasliId;
    private int _digerYasliId;

    public HatirlatmaTestleri()
    {
        var c = _db.Context;
        var hatirlatmaRepo = new EfHatirlatmaRepository(c);
        var aktiviteRepo = new EfAktiviteKaydiRepository(c);
        _hatirlatma = new HatirlatmaManager(hatirlatmaRepo, aktiviteRepo, new EfYasliRepository(c), _saat);
        _takip = new HatirlatmaTakipManager(hatirlatmaRepo, aktiviteRepo, new EfOlayRepository(c), _saat);

        var y1 = new Yasli { Ad = "Test", Soyad = "Bir", DogumTarihi = new DateOnly(1945, 1, 1) };
        var y2 = new Yasli { Ad = "Test", Soyad = "İki", DogumTarihi = new DateOnly(1945, 1, 1) };
        c.Yaslilar.AddRange(y1, y2);
        c.SaveChanges();
        _yasliId = y1.Id;
        _digerYasliId = y2.Id;
    }

    public void Dispose() => _db.Dispose();

    private async Task<Hatirlatma> EkleAsync(HatirlatmaTuru tur = HatirlatmaTuru.Ilac, string baslik = "Tansiyon ilacı",
        TekrarTipi tekrar = TekrarTipi.Gunluk)
    {
        var h = new Hatirlatma
        {
            YasliId = _yasliId, Baslik = baslik, Tur = tur, Saat = new TimeOnly(8, 0),
            Tekrar = tekrar, BaslangicTarihi = Bugun,
        };
        await _hatirlatma.EkleAsync(h);
        return h;
    }

    // --- Doğrulama ---

    [Fact]
    public async Task Ekle_GecmisBaslangicReddedilir()
    {
        var h = new Hatirlatma { YasliId = _yasliId, Baslik = "X", Saat = new TimeOnly(8, 0), Tekrar = TekrarTipi.Gunluk,
                                 Tur = HatirlatmaTuru.Ilac, BaslangicTarihi = Bugun.AddDays(-1) };
        await Assert.ThrowsAsync<ArgumentException>(() => _hatirlatma.EkleAsync(h));
    }

    [Fact]
    public async Task Ekle_AyniSaatteAyniBaslikIkinciKezEklenemez()
    {
        await EkleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => EkleAsync());
        // Farklı başlık aynı saatte olabilir.
        await EkleAsync(baslik: "Su iç", tur: HatirlatmaTuru.SuIcme);
    }

    [Fact]
    public async Task Ekle_GecersizEnumDegeriReddedilir()
    {
        var h = new Hatirlatma { YasliId = _yasliId, Baslik = "X", Saat = new TimeOnly(8, 0), Tekrar = (TekrarTipi)42,
                                 Tur = HatirlatmaTuru.Ilac, BaslangicTarihi = Bugun };
        await Assert.ThrowsAsync<ArgumentException>(() => _hatirlatma.EkleAsync(h));
    }

    [Fact]
    public async Task Ekle_TekSeferlikteBitisBaslangicaEsitlenir()
    {
        var h = await EkleAsync(tekrar: TekrarTipi.Tek, tur: HatirlatmaTuru.Randevu, baslik: "Kontrol");
        Assert.Equal(h.BaslangicTarihi, h.BitisTarihi);
    }

    [Fact]
    public async Task Guncelle_YasliIdDegistirilemez()
    {
        var h = await EkleAsync();
        _db.Context.ChangeTracker.Clear();

        var girdi = new Hatirlatma
        {
            Id = h.Id, YasliId = _digerYasliId,            // başka yaşlıya taşıma denemesi
            Baslik = "Tansiyon ilacı (sabah)", Tur = h.Tur, Saat = h.Saat, Tekrar = h.Tekrar, BaslangicTarihi = h.BaslangicTarihi,
        };
        await _hatirlatma.GuncelleAsync(girdi);
        _db.Context.ChangeTracker.Clear();

        var kayitli = (await _hatirlatma.GetirAsync(h.Id))!;
        Assert.Equal("Tansiyon ilacı (sabah)", kayitli.Baslik);
        Assert.Equal(_yasliId, kayitli.YasliId);
    }

    // --- Onay / geri bildirim ---

    [Fact]
    public async Task Onay_ErkenYapilamaz()
    {
        var h = await EkleAsync();                        // saat 06:30, ilaç 08:00
        await Assert.ThrowsAsync<InvalidOperationException>(() => _takip.YanitlaAsync(_yasliId, h.Id, Bugun0800, alindi: true));
    }

    [Fact]
    public async Task Onay_ZamanindaTamamlandi_GecGecikti()
    {
        var sabah = await EkleAsync();
        var aksam = await EkleAsync(baslik: "Akşam ilacı");

        _saat.Ayarla(Bugun0800.AddMinutes(10));
        var k1 = await _takip.YanitlaAsync(_yasliId, sabah.Id, Bugun0800, alindi: true);
        _saat.Ayarla(Bugun0800.AddMinutes(75));
        var k2 = await _takip.YanitlaAsync(_yasliId, aksam.Id, Bugun0800, alindi: true);

        Assert.Equal(AktiviteDurumu.Tamamlandi, k1.Durum);
        Assert.Equal(AktiviteDurumu.Gecikti, k2.Durum);
    }

    [Fact]
    public async Task Onay_AyniDozIkiKezYanitlanamaz()
    {
        var h = await EkleAsync();
        _saat.Ayarla(Bugun0800);
        await _takip.YanitlaAsync(_yasliId, h.Id, Bugun0800, alindi: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _takip.YanitlaAsync(_yasliId, h.Id, Bugun0800, alindi: false));
    }

    [Fact]
    public async Task Onay_BaskaYaslininHatirlatmasiYanitlanamaz()
    {
        var h = await EkleAsync();
        _saat.Ayarla(Bugun0800);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _takip.YanitlaAsync(_digerYasliId, h.Id, Bugun0800, alindi: true));
    }

    [Fact]
    public async Task Onay_PlanlanmamisBirSaatGonderilemez()
    {
        var h = await EkleAsync();
        _saat.Ayarla(Bugun0800);
        await Assert.ThrowsAsync<ArgumentException>(() => _takip.YanitlaAsync(_yasliId, h.Id, Bugun0800.AddMinutes(5), alindi: true));
    }

    [Fact]
    public async Task Atlanan_IlacAlarmUretir_SuIcmeUretmez()
    {
        var ilac = await EkleAsync();
        var su = await EkleAsync(tur: HatirlatmaTuru.SuIcme, baslik: "Su iç");
        _saat.Ayarla(Bugun0800);

        await _takip.YanitlaAsync(_yasliId, ilac.Id, Bugun0800, alindi: false);
        await _takip.YanitlaAsync(_yasliId, su.Id, Bugun0800, alindi: false);

        var olay = Assert.Single(await _db.Context.Olaylar.ToListAsync());
        Assert.Equal(OlayTuru.KacirilanHatirlatma, olay.Tur);
        Assert.Equal(OlaySeviyesi.Yuksek, olay.Seviye);
    }

    // --- Onaylanmazsa: kaçırma ---

    [Fact]
    public async Task Onaylanmazsa_IkiSaatSonraKacirildiVeAlarm()
    {
        var h = await EkleAsync();

        _saat.Ayarla(Bugun0800.AddMinutes(119));
        Assert.Equal(0, await _takip.KacirilanlariIsleAsync());     // süre dolmadı

        _saat.Ayarla(Bugun0800.AddHours(2));
        Assert.Equal(1, await _takip.KacirilanlariIsleAsync());
        Assert.Equal(0, await _takip.KacirilanlariIsleAsync());     // ikinci çalıştırmada tekrar işaretlemez

        var kayit = await _db.Context.AktiviteKayitlari.SingleAsync();
        Assert.Equal(AktiviteDurumu.Kacirildi, kayit.Durum);
        Assert.Equal(Bugun0800, kayit.PlanlananZaman);
        Assert.Single(await _db.Context.Olaylar.ToListAsync());

        // Süre dolduktan sonra artık onaylanamaz.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _takip.YanitlaAsync(_yasliId, h.Id, Bugun0800, alindi: true));
    }

    [Fact]
    public async Task Kacirma_YanitlanmisDozuVeOlusturulmadanOncekiDozuIsaretlemez()
    {
        // 06:30'da eklendi; dünün 08:00 dozu hatırlatma oluşturulmadan önceydi.
        var h = await EkleAsync();
        _saat.Ayarla(Bugun0800);
        await _takip.YanitlaAsync(_yasliId, h.Id, Bugun0800, alindi: true);

        _saat.Ayarla(Bugun0800.AddHours(5));
        Assert.Equal(0, await _takip.KacirilanlariIsleAsync());
    }

    // --- Günlük plan ve uyum ---

    [Fact]
    public async Task GunlukPlan_DurumlarDogruHesaplanir()
    {
        var alinan = await EkleAsync(baslik: "Alınan");
        await EkleAsync(baslik: "Bekleyen");
        _saat.Ayarla(Bugun0800.AddMinutes(5));
        await _takip.YanitlaAsync(_yasliId, alinan.Id, Bugun0800, alindi: true);

        var plan = await _takip.GunlukPlanAsync(_yasliId, Bugun);

        Assert.Equal(PlanDurumu.Tamamlandi, plan.Single(p => p.Hatirlatma.Baslik == "Alınan").Durum);
        var bekleyen = plan.Single(p => p.Hatirlatma.Baslik == "Bekleyen");
        Assert.Equal(PlanDurumu.OnayBekleniyor, bekleyen.Durum);
        Assert.True(bekleyen.OnaylanabilirMi);
    }

    [Fact]
    public async Task UyumOzeti_AlinanlarinOrani()
    {
        var a = await EkleAsync(baslik: "A");
        var b = await EkleAsync(baslik: "B");
        var c = await EkleAsync(baslik: "C");
        await EkleAsync(baslik: "D");
        _saat.Ayarla(Bugun0800.AddMinutes(5));
        await _takip.YanitlaAsync(_yasliId, a.Id, Bugun0800, alindi: true);
        _saat.Ayarla(Bugun0800.AddMinutes(60));
        await _takip.YanitlaAsync(_yasliId, b.Id, Bugun0800, alindi: true);
        await _takip.YanitlaAsync(_yasliId, c.Id, Bugun0800, alindi: false);
        _saat.Ayarla(Bugun0800.AddHours(3));
        await _takip.KacirilanlariIsleAsync();                    // D kaçırıldı

        var uyum = await _takip.UyumOzetiAsync(_yasliId);

        Assert.Equal(new UyumOzeti(Toplam: 4, Zamaninda: 1, Gec: 1, Atlanan: 1, Kacirilan: 1), uyum);
        Assert.Equal(50, uyum.Oran);
    }

    [Fact]
    public async Task UniqueIndeks_AyniDozIcinIkinciKayitVeritabanindaEngellenir()
    {
        var h = await EkleAsync();
        AktiviteKaydi Kayit() => new()
        {
            YasliId = _yasliId, HatirlatmaId = h.Id, Tur = AktiviteTuru.HatirlatmaOnayi,
            Durum = AktiviteDurumu.Tamamlandi, PlanlananZaman = Bugun0800,
        };
        _db.Context.AktiviteKayitlari.AddRange(Kayit(), Kayit());
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }
}
