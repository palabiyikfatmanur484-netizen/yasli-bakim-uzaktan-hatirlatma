using YasliBakim.Business.Zamanlama;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Tests;

// Veritabanı gerektirmeyen, saf tekrar hesaplama testleri.
public class ZamanlayiciTestleri
{
    private static Hatirlatma H(TekrarTipi tekrar, DateOnly baslangic, DateOnly? bitis = null, bool aktif = true) =>
        new() { Baslik = "Test", Saat = new TimeOnly(8, 30), Tekrar = tekrar, BaslangicTarihi = baslangic, BitisTarihi = bitis, AktifMi = aktif };

    private static readonly DateOnly Pazartesi = new(2026, 11, 2);

    [Fact]
    public void Gunluk_HerGunGerceklesir_BaslangictanOnceGerceklesmez()
    {
        var h = H(TekrarTipi.Gunluk, Pazartesi);

        Assert.False(HatirlatmaZamanlayici.GerceklesirMi(h, Pazartesi.AddDays(-1)));
        Assert.All(Enumerable.Range(0, 10), i => Assert.True(HatirlatmaZamanlayici.GerceklesirMi(h, Pazartesi.AddDays(i))));
    }

    [Fact]
    public void Haftalik_YalnizcaBaslangicGunundeGerceklesir()
    {
        var h = H(TekrarTipi.Haftalik, Pazartesi);
        var tekrarlar = HatirlatmaZamanlayici.Tekrarlar(h, Pazartesi, Pazartesi.AddDays(20)).ToList();

        Assert.Equal(3, tekrarlar.Count);
        Assert.All(tekrarlar, t => Assert.Equal(DayOfWeek.Monday, t.DayOfWeek));
    }

    [Fact]
    public void Aylik_AyinOtuzBirindeBaslarsa_KisaAylardaSonGunGerceklesir()
    {
        var h = H(TekrarTipi.Aylik, new DateOnly(2026, 1, 31));

        Assert.True(HatirlatmaZamanlayici.GerceklesirMi(h, new DateOnly(2026, 2, 28)));   // Şubat'ın son günü
        Assert.False(HatirlatmaZamanlayici.GerceklesirMi(h, new DateOnly(2026, 2, 27)));
        Assert.True(HatirlatmaZamanlayici.GerceklesirMi(h, new DateOnly(2026, 4, 30)));   // Nisan 30 çeker
        Assert.True(HatirlatmaZamanlayici.GerceklesirMi(h, new DateOnly(2026, 5, 31)));
    }

    [Fact]
    public void Tek_YalnizcaBirKezGerceklesir()
    {
        var h = H(TekrarTipi.Tek, Pazartesi);
        Assert.Single(HatirlatmaZamanlayici.Tekrarlar(h, Pazartesi.AddDays(-5), Pazartesi.AddDays(30)));
    }

    [Fact]
    public void BitisTarihindenSonraVePasifkenGerceklesmez()
    {
        var bitisli = H(TekrarTipi.Gunluk, Pazartesi, bitis: Pazartesi.AddDays(2));
        var pasif = H(TekrarTipi.Gunluk, Pazartesi, aktif: false);

        Assert.True(HatirlatmaZamanlayici.GerceklesirMi(bitisli, Pazartesi.AddDays(2)));
        Assert.False(HatirlatmaZamanlayici.GerceklesirMi(bitisli, Pazartesi.AddDays(3)));
        Assert.False(HatirlatmaZamanlayici.GerceklesirMi(pasif, Pazartesi));
    }

    [Fact]
    public void SonrakiTekrar_SaatGectiyseErtesiGunuVerir()
    {
        var h = H(TekrarTipi.Gunluk, Pazartesi);

        Assert.Equal(Pazartesi.ToDateTime(new TimeOnly(8, 30)), HatirlatmaZamanlayici.SonrakiTekrar(h, Pazartesi.ToDateTime(new TimeOnly(7, 0))));
        Assert.Equal(Pazartesi.AddDays(1).ToDateTime(new TimeOnly(8, 30)), HatirlatmaZamanlayici.SonrakiTekrar(h, Pazartesi.ToDateTime(new TimeOnly(9, 0))));
        Assert.Null(HatirlatmaZamanlayici.SonrakiTekrar(H(TekrarTipi.Tek, Pazartesi), Pazartesi.ToDateTime(new TimeOnly(9, 0))));
    }

    [Theory]
    [InlineData(0, true)]      // tam zamanında
    [InlineData(30, true)]     // 30. dakika hâlâ zamanında
    [InlineData(31, false)]    // sonrası geç
    public void AlindiDurumu_OtuzDakikaSiniri(int dakika, bool zamanindaMi)
    {
        var planlanan = Pazartesi.ToDateTime(new TimeOnly(8, 30));
        var durum = HatirlatmaKurallari.AlindiDurumu(planlanan, planlanan.AddMinutes(dakika));
        Assert.Equal(zamanindaMi ? AktiviteDurumu.Tamamlandi : AktiviteDurumu.Gecikti, durum);
    }

    [Theory]
    [InlineData(-61, false)]   // 1 saatten erken: henüz açık değil
    [InlineData(-60, true)]
    [InlineData(119, true)]
    [InlineData(120, false)]   // 2 saat doldu: kaçırıldı
    public void OnayPenceresi_BirSaatOnceAcilirIkiSaatSonraKapanir(int dakika, bool acikMi)
    {
        var planlanan = Pazartesi.ToDateTime(new TimeOnly(8, 30));
        Assert.Equal(acikMi, HatirlatmaKurallari.OnayPenceresiAcikMi(planlanan, planlanan.AddMinutes(dakika)));
    }
}
