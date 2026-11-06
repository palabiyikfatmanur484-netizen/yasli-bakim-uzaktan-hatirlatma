using Microsoft.EntityFrameworkCore;
using YasliBakim.Business.Abstract;
using YasliBakim.Business.Concrete;
using YasliBakim.Business.Kurallar;
using YasliBakim.DataAccess.Concrete;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Tests;

// Günlük durum ve aktivite kaydı modülü: doğrulama, günde bir durum, anomali, sahiplik ve geçmiş.
public class AktiviteTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private static readonly DateOnly Bugun = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateTime Bugun1200 = Bugun.ToDateTime(new TimeOnly(12, 0));
    private readonly SabitSaat _saat = new(Bugun1200);
    private readonly AktiviteManager _aktivite;
    private readonly int _yasliId;
    private readonly int _digerYasliId;
    private readonly int _bakiciId;
    private readonly int _digerBakiciId;

    public AktiviteTestleri()
    {
        var c = _db.Context;
        _aktivite = new AktiviteManager(new EfAktiviteKaydiRepository(c), new EfOlayRepository(c), new EfYasliRepository(c), _saat);

        var y1 = new Yasli { Ad = "Test", Soyad = "Bir", DogumTarihi = new DateOnly(1945, 1, 1) };
        var y2 = new Yasli { Ad = "Test", Soyad = "İki", DogumTarihi = new DateOnly(1945, 1, 1) };
        var b1 = new Bakici { Ad = "B", Soyad = "Bir", Eposta = "b1@ornek.test", Telefon = "0", Rol = BakiciRolu.ProfesyonelBakici };
        var b2 = new Bakici { Ad = "B", Soyad = "İki", Eposta = "b2@ornek.test", Telefon = "0", Rol = BakiciRolu.AileYakini };
        c.AddRange(y1, y2, b1, b2);
        c.SaveChanges();
        (_yasliId, _digerYasliId, _bakiciId, _digerBakiciId) = (y1.Id, y2.Id, b1.Id, b2.Id);
    }

    public void Dispose() => _db.Dispose();

    private Task<AktiviteKaydi> AktiviteEkleAsync(DateTime? zaman = null, int? bakiciId = null,
        AktiviteKategorisi kategori = AktiviteKategorisi.Yuruyus, int sure = 30) =>
        _aktivite.AktiviteEkleAsync(new AktiviteGirdisi(_yasliId, bakiciId, kategori, sure, zaman ?? Bugun1200.AddHours(-1)));

    // --- Günlük durum ---

    [Fact]
    public async Task GunlukDurum_AyniGunIkinciBildirimYeniSatirAcmazGunceller()
    {
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Normal, agriSeviyesi: 2);
        _saat.IleriAl(TimeSpan.FromHours(3));
        var ikinci = await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Iyi, not: "Yürüdükten sonra");

        var kayit = await _db.Context.AktiviteKayitlari.SingleAsync(a => a.Tur == AktiviteTuru.GunlukDurum);
        Assert.Equal(ikinci.Id, kayit.Id);
        Assert.Equal(RuhHali.Iyi, kayit.RuhHali);
        Assert.Null(kayit.AgriSeviyesi);
        Assert.Equal(Bugun1200.AddHours(3), kayit.KayitZamani);
    }

    [Fact]
    public async Task GunlukDurum_ErtesiGunYeniKayitAcilir()
    {
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Normal);
        _saat.IleriAl(TimeSpan.FromDays(1));
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Iyi);

        Assert.Equal(2, await _db.Context.AktiviteKayitlari.CountAsync(a => a.Tur == AktiviteTuru.GunlukDurum));
    }

    [Theory]
    [InlineData(RuhHali.Iyi, null, null)]
    [InlineData(RuhHali.Normal, 6, null)]
    [InlineData(RuhHali.Kotu, null, OlaySeviyesi.Orta)]
    [InlineData(RuhHali.CokKotu, null, OlaySeviyesi.Yuksek)]
    [InlineData(RuhHali.Iyi, 7, OlaySeviyesi.Orta)]
    [InlineData(RuhHali.Kotu, 9, OlaySeviyesi.Yuksek)]       // hangisi daha kötüyse
    [InlineData(RuhHali.CokKotu, 7, OlaySeviyesi.Yuksek)]
    public void AnomaliSeviyesi_RuhHaliVeAgridanKotuOlaniAlir(RuhHali ruh, int? agri, OlaySeviyesi? beklenen) =>
        Assert.Equal(beklenen, AktiviteKurallari.AnomaliSeviyesi(ruh, agri));

    [Fact]
    public async Task GunlukDurum_KotuBildirimOlayUretirAmaGundeBirKez()
    {
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Kotu, not: "Başım dönüyor");
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.CokKotu);

        var olay = await _db.Context.Olaylar.SingleAsync();
        Assert.Equal(OlayTuru.Anomali, olay.Tur);
        Assert.Equal(OlaySeviyesi.Orta, olay.Seviye);
        Assert.Contains("Kötü", olay.Aciklama);
        Assert.Contains("Başım dönüyor", olay.Aciklama);
    }

    [Fact]
    public async Task GunlukDurum_IyiyeDonulunceOlaySilinmez()
    {
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Iyi, agriSeviyesi: 8);
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Iyi, agriSeviyesi: 1);

        Assert.Equal(1, await _db.Context.Olaylar.CountAsync());
    }

    [Fact]
    public async Task GunlukDurum_NormalBildirimOlayUretmez()
    {
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Normal, agriSeviyesi: 3);
        Assert.Equal(0, await _db.Context.Olaylar.CountAsync());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task GunlukDurum_AgriSeviyesiSinirDisindaReddedilir(int agri) =>
        await Assert.ThrowsAsync<ArgumentException>(() => _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Normal, agri));

    [Fact]
    public async Task GunlukDurum_GecersizRuhHaliVeUzunNotReddedilir()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _aktivite.GunlukDurumBildirAsync(_yasliId, (RuhHali)42));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Normal, not: new string('x', 501)));
        Assert.Equal(0, await _db.Context.AktiviteKayitlari.CountAsync());
    }

    [Fact]
    public async Task BugunkuDurum_YalnizcaBuguneVeBuYasliyaBakar()
    {
        await _aktivite.GunlukDurumBildirAsync(_digerYasliId, RuhHali.Iyi);
        Assert.Null(await _aktivite.BugunkuDurumAsync(_yasliId));

        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Normal);
        _saat.IleriAl(TimeSpan.FromDays(1));
        Assert.Null(await _aktivite.BugunkuDurumAsync(_yasliId));
    }

    // --- Aktivite ---

    [Fact]
    public async Task Aktivite_EklenirVeSaniyelerAtilir()
    {
        var kayit = await AktiviteEkleAsync(zaman: Bugun1200.AddMinutes(-30).AddSeconds(42), bakiciId: _bakiciId);

        Assert.Equal(AktiviteTuru.Aktivite, kayit.Tur);
        Assert.Equal(Bugun1200.AddMinutes(-30), kayit.KayitZamani);
        Assert.Equal(Bugun1200, kayit.OlusturmaTarihi);        // giriş anı ayrı tutulur
        Assert.Equal(_bakiciId, kayit.KaydedenBakiciId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(481)]
    public async Task Aktivite_SureSinirDisindaReddedilir(int sure) =>
        await Assert.ThrowsAsync<ArgumentException>(() => AktiviteEkleAsync(sure: sure));

    [Fact]
    public async Task Aktivite_GelecekZamanReddedilirKucukSaatFarkinaIzinVerilir()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => AktiviteEkleAsync(zaman: Bugun1200.AddMinutes(30)));
        await AktiviteEkleAsync(zaman: Bugun1200.AddMinutes(3));
    }

    [Fact]
    public async Task Aktivite_IkiGundenEskiZamanReddedilir()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => AktiviteEkleAsync(zaman: Bugun1200.AddHours(-49)));
        await AktiviteEkleAsync(zaman: Bugun1200.AddHours(-47));
    }

    [Fact]
    public async Task Aktivite_AyniDakikadaAyniAktiviteIkiKezKaydedilmez()
    {
        await AktiviteEkleAsync(zaman: Bugun1200.AddMinutes(-10));
        await Assert.ThrowsAsync<InvalidOperationException>(() => AktiviteEkleAsync(zaman: Bugun1200.AddMinutes(-10).AddSeconds(30)));
        // Aynı saatte farklı bir aktivite olabilir.
        await AktiviteEkleAsync(zaman: Bugun1200.AddMinutes(-10), kategori: AktiviteKategorisi.Sosyal);
    }

    [Fact]
    public async Task Aktivite_GecersizKategoriVeOlmayanYasliReddedilir()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => AktiviteEkleAsync(kategori: (AktiviteKategorisi)42));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _aktivite.AktiviteEkleAsync(new AktiviteGirdisi(9999, null, AktiviteKategorisi.Hobi, 30, Bugun1200)));
    }

    // --- Silme ve sahiplik ---

    [Fact]
    public async Task Sil_KaydiGirenKisiSilebilir()
    {
        var yasliKaydi = await AktiviteEkleAsync();
        var bakiciKaydi = await AktiviteEkleAsync(bakiciId: _bakiciId, kategori: AktiviteKategorisi.Sosyal);

        await _aktivite.AktiviteSilAsync(yasliKaydi.Id, _yasliId, bakiciId: null);
        await _aktivite.AktiviteSilAsync(bakiciKaydi.Id, _yasliId, _bakiciId);

        Assert.Equal(0, await _db.Context.AktiviteKayitlari.CountAsync());
    }

    [Fact]
    public async Task Sil_BaskasininGirdigiKayitSilinemez()
    {
        var bakiciKaydi = await AktiviteEkleAsync(bakiciId: _bakiciId);

        // Aynı yaşlının başka bakıcısı da, yaşlının kendisi de silemez.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _aktivite.AktiviteSilAsync(bakiciKaydi.Id, _yasliId, _digerBakiciId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _aktivite.AktiviteSilAsync(bakiciKaydi.Id, _yasliId, null));
        // Kayıt başka yaşlıya aitse (adres çubuğunda yasliId değiştirme) reddedilir.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _aktivite.AktiviteSilAsync(bakiciKaydi.Id, _digerYasliId, _bakiciId));
        Assert.Equal(1, await _db.Context.AktiviteKayitlari.CountAsync());
    }

    [Fact]
    public async Task Sil_YirmiDortSaattenEskiKayitSilinemez()
    {
        var kayit = await AktiviteEkleAsync();
        _saat.IleriAl(TimeSpan.FromHours(25));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _aktivite.AktiviteSilAsync(kayit.Id, _yasliId, null));
    }

    [Fact]
    public async Task Sil_GunlukDurumVeHatirlatmaOnayiSilinemez()
    {
        var durum = await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Kotu);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _aktivite.AktiviteSilAsync(durum.Id, _yasliId, null));
        Assert.Equal(1, await _db.Context.Olaylar.CountAsync());
    }

    // --- Geçmiş ---

    [Fact]
    public async Task Gecmis_AraligaVeTureGoreFiltrelerEnYeniOnce()
    {
        await AktiviteEkleAsync(zaman: Bugun1200.AddDays(-1));
        await AktiviteEkleAsync(zaman: Bugun1200.AddHours(-2));
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Iyi);
        await _aktivite.GunlukDurumBildirAsync(_digerYasliId, RuhHali.Iyi);

        var bugunku = await _aktivite.GecmisAsync(_yasliId, Bugun, Bugun);
        Assert.Equal(2, bugunku.Count);
        Assert.True(bugunku[0].KayitZamani > bugunku[1].KayitZamani);

        var aktiviteler = await _aktivite.GecmisAsync(_yasliId, Bugun.AddDays(-1), Bugun, AktiviteTuru.Aktivite);
        Assert.Equal(2, aktiviteler.Count);
        Assert.All(aktiviteler, a => Assert.Equal(AktiviteTuru.Aktivite, a.Tur));
    }

    [Fact]
    public async Task Gecmis_GecersizAralikReddedilir()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _aktivite.GecmisAsync(_yasliId, Bugun, Bugun.AddDays(-1)));
        await Assert.ThrowsAsync<ArgumentException>(() => _aktivite.GecmisAsync(_yasliId, Bugun.AddDays(-31), Bugun));
        await _aktivite.GecmisAsync(_yasliId, Bugun.AddDays(-30), Bugun);     // 31 gün: sınırda
    }

    [Fact]
    public async Task GunlukOzet_GunGunHesaplanirDozPlanlananGuneSayilir()
    {
        var h = new Hatirlatma { YasliId = _yasliId, Baslik = "Gece ilacı", Tur = HatirlatmaTuru.Ilac, Saat = new TimeOnly(23, 30),
                                 Tekrar = TekrarTipi.Gunluk, BaslangicTarihi = Bugun.AddDays(-2) };
        _db.Context.Hatirlatmalar.Add(h);
        await _db.Context.SaveChangesAsync();
        // Dünkü 23:30 dozu bugün 01:30'da kaçırıldı olarak işaretlendi: yine de dünün dozudur.
        _db.Context.AktiviteKayitlari.Add(new AktiviteKaydi
        {
            YasliId = _yasliId, HatirlatmaId = h.Id, Tur = AktiviteTuru.HatirlatmaOnayi, Durum = AktiviteDurumu.Kacirildi,
            PlanlananZaman = Bugun.AddDays(-1).ToDateTime(new TimeOnly(23, 30)), KayitZamani = Bugun.ToDateTime(new TimeOnly(1, 30)),
        });
        await _db.Context.SaveChangesAsync();

        await AktiviteEkleAsync(zaman: Bugun1200.AddHours(-3), sure: 30);
        await AktiviteEkleAsync(zaman: Bugun1200.AddHours(-1), sure: 45, kategori: AktiviteKategorisi.Hobi);
        await _aktivite.GunlukDurumBildirAsync(_yasliId, RuhHali.Iyi, agriSeviyesi: 2);

        var ozet = await _aktivite.GunlukOzetAsync(_yasliId, Bugun.AddDays(-2), Bugun);

        Assert.Equal([Bugun, Bugun.AddDays(-1), Bugun.AddDays(-2)], ozet.Select(g => g.Gun));
        Assert.Equal(RuhHali.Iyi, ozet[0].Durum?.RuhHali);
        Assert.Equal((2, 75, 0), (ozet[0].AktiviteSayisi, ozet[0].AktiviteDakika, ozet[0].YanitlananDoz));
        Assert.Equal((0, 1), (ozet[1].AlinanDoz, ozet[1].YanitlananDoz));
        Assert.Null(ozet[2].Durum);
    }

    // --- Veritabanı kısıtları (son savunma) ---

    [Fact]
    public async Task CheckKisiti_AktivitedeKategoriVeSureZorunlu()
    {
        _db.Context.AktiviteKayitlari.Add(new AktiviteKaydi { YasliId = _yasliId, Tur = AktiviteTuru.Aktivite, SureDakika = 10 });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task CheckKisiti_AgriVeSureAraligi()
    {
        _db.Context.AktiviteKayitlari.Add(new AktiviteKaydi { YasliId = _yasliId, Tur = AktiviteTuru.GunlukDurum, RuhHali = RuhHali.Iyi, AgriSeviyesi = 11 });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
        _db.Context.ChangeTracker.Clear();

        _db.Context.AktiviteKayitlari.Add(new AktiviteKaydi { YasliId = _yasliId, Tur = AktiviteTuru.Aktivite, Kategori = AktiviteKategorisi.Hobi, SureDakika = 0 });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task BakiciSilinirseGirdigiAktiviteKalirKaydedenBosalir()
    {
        var kayit = await AktiviteEkleAsync(bakiciId: _bakiciId);
        _db.Context.Bakicilar.Remove(await _db.Context.Bakicilar.FindAsync(_bakiciId) ?? throw new InvalidOperationException());
        await _db.Context.SaveChangesAsync();
        _db.Context.ChangeTracker.Clear();

        var kalan = await _db.Context.AktiviteKayitlari.SingleAsync(a => a.Id == kayit.Id);
        Assert.Null(kalan.KaydedenBakiciId);
    }
}
