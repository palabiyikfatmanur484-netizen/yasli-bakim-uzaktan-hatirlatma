using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Context;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.DataAccess.Seed;

/// <summary>
/// Veritabanını sentetik (uydurma) örnek veriyle doldurur.
/// KVKK: İsimler, telefonlar ve adresler hayalidir; telefonlar 0500 000 xx xx aralığında,
/// e-postalar ise gerçek bir alan adı olmayan "ornek.test" altındadır.
/// </summary>
public static class OrnekVeri
{
    public static async Task YukleAsync(YasliBakimDbContext context)
    {
        // Tablo doluysa tekrar eklemez (uygulama her açıldığında çağrılabilir).
        if (await context.Yaslilar.AnyAsync())
            return;

        var bugun = DateOnly.FromDateTime(DateTime.Today);

        // --- Yaşlılar ---
        var ayse = new Yasli { Ad = "Ayşe", Soyad = "Örnekoğlu", DogumTarihi = new DateOnly(1944, 3, 12), Telefon = "0500 000 10 01", Adres = "Örnek Mah. Deneme Sk. No:1, Ankara", KronikHastaliklar = "Hipertansiyon, Tip 2 diyabet", AcilDurumNotu = "Penisilin alerjisi var." };
        var mehmet = new Yasli { Ad = "Mehmet", Soyad = "Testçi", DogumTarihi = new DateOnly(1940, 11, 5), Telefon = "0500 000 10 02", Adres = "Kurgu Mah. Hayal Cd. No:7, İzmir", KronikHastaliklar = "KOAH", AcilDurumNotu = "Oksijen cihazı yatak odasında." };
        var fatma = new Yasli { Ad = "Fatma", Soyad = "Sentetik", DogumTarihi = new DateOnly(1948, 6, 30), Telefon = "0500 000 10 03", Adres = "Deneme Mah. Örnek Sk. No:12, Bursa", KronikHastaliklar = "Osteoporoz" };
        var hasan = new Yasli { Ad = "Hasan", Soyad = "Uydurma", DogumTarihi = new DateOnly(1937, 1, 20), Telefon = "0500 000 10 04", Adres = "Hayal Mah. Kurgu Sk. No:3, Konya", KronikHastaliklar = "Alzheimer (erken evre), Hipertansiyon", AcilDurumNotu = "Evden yalnız çıkmamalı." };
        var zeynep = new Yasli { Ad = "Zeynep", Soyad = "Deneme", DogumTarihi = new DateOnly(1951, 9, 8), Telefon = "0500 000 10 05", Adres = "Test Mah. Model Cd. No:21, Eskişehir" };

        // --- Bakıcılar / aile yakınları ---
        var elif = new Bakici { Ad = "Elif", Soyad = "Örnekoğlu", Eposta = "elif@ornek.test", Telefon = "0500 000 20 01", Rol = BakiciRolu.AileYakini };
        var can = new Bakici { Ad = "Can", Soyad = "Testçi", Eposta = "can@ornek.test", Telefon = "0500 000 20 02", Rol = BakiciRolu.AileYakini };
        var selin = new Bakici { Ad = "Selin", Soyad = "Bakımcı", Eposta = "selin@ornek.test", Telefon = "0500 000 20 03", Rol = BakiciRolu.ProfesyonelBakici };
        var murat = new Bakici { Ad = "Murat", Soyad = "Hemşireoğlu", Eposta = "murat@ornek.test", Telefon = "0500 000 20 04", Rol = BakiciRolu.ProfesyonelBakici };

        // --- N-N eşleştirmeler (YasliBakici) ---
        // Selin üç yaşlıya bakıyor, Ayşe'yi hem kızı Elif hem Selin takip ediyor.
        var eslesmeler = new List<YasliBakici>
        {
            new() { Yasli = ayse,   Bakici = elif,  YakinlikDerecesi = "Kızı",    BirincilSorumluMu = true },
            new() { Yasli = ayse,   Bakici = selin, YakinlikDerecesi = null,      BirincilSorumluMu = false },
            new() { Yasli = mehmet, Bakici = can,   YakinlikDerecesi = "Oğlu",    BirincilSorumluMu = true },
            new() { Yasli = mehmet, Bakici = murat, YakinlikDerecesi = null,      BirincilSorumluMu = false },
            new() { Yasli = fatma,  Bakici = selin, YakinlikDerecesi = null,      BirincilSorumluMu = true },
            new() { Yasli = hasan,  Bakici = murat, YakinlikDerecesi = null,      BirincilSorumluMu = true },
            new() { Yasli = hasan,  Bakici = can,   YakinlikDerecesi = "Yeğeni",  BirincilSorumluMu = false },
            new() { Yasli = zeynep, Bakici = selin, YakinlikDerecesi = null,      BirincilSorumluMu = true },
        };

        // --- Hatırlatmalar ---
        var basla = bugun.AddDays(-14);
        var hatirlatmalar = new List<Hatirlatma>
        {
            new() { Yasli = ayse,   OlusturanBakici = elif,  Baslik = "Tansiyon ilacı",       Tur = HatirlatmaTuru.Ilac,     Saat = new TimeOnly(8, 0),   Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla },
            new() { Yasli = ayse,   OlusturanBakici = elif,  Baslik = "Metformin (akşam)",    Tur = HatirlatmaTuru.Ilac,     Saat = new TimeOnly(19, 0),  Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla, Aciklama = "Yemekten sonra" },
            new() { Yasli = ayse,   OlusturanBakici = selin, Baslik = "Su iç",                Tur = HatirlatmaTuru.SuIcme,   Saat = new TimeOnly(11, 0),  Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla },
            new() { Yasli = ayse,   OlusturanBakici = elif,  Baslik = "Dahiliye kontrolü",    Tur = HatirlatmaTuru.Randevu,  Saat = new TimeOnly(10, 30), Tekrar = TekrarTipi.Tek,      BaslangicTarihi = bugun.AddDays(5), BitisTarihi = bugun.AddDays(5) },
            new() { Yasli = mehmet, OlusturanBakici = can,   Baslik = "İnhaler",              Tur = HatirlatmaTuru.Ilac,     Saat = new TimeOnly(9, 0),   Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla },
            new() { Yasli = mehmet, OlusturanBakici = murat, Baslik = "Nefes egzersizi",      Tur = HatirlatmaTuru.Egzersiz, Saat = new TimeOnly(16, 0),  Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla },
            new() { Yasli = fatma,  OlusturanBakici = selin, Baslik = "D vitamini",           Tur = HatirlatmaTuru.Ilac,     Saat = new TimeOnly(9, 30),  Tekrar = TekrarTipi.Haftalik, BaslangicTarihi = basla },
            new() { Yasli = fatma,  OlusturanBakici = selin, Baslik = "Öğle yemeği",          Tur = HatirlatmaTuru.Yemek,    Saat = new TimeOnly(12, 30), Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla },
            new() { Yasli = hasan,  OlusturanBakici = murat, Baslik = "Donepezil",            Tur = HatirlatmaTuru.Ilac,     Saat = new TimeOnly(21, 0),  Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla },
            new() { Yasli = hasan,  OlusturanBakici = murat, Baslik = "Tansiyon ilacı",       Tur = HatirlatmaTuru.Ilac,     Saat = new TimeOnly(8, 30),  Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla },
            new() { Yasli = hasan,  OlusturanBakici = null,  Baslik = "Eski antibiyotik kürü", Tur = HatirlatmaTuru.Ilac,    Saat = new TimeOnly(12, 0),  Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla, BitisTarihi = bugun.AddDays(-7), AktifMi = false },
            new() { Yasli = zeynep, OlusturanBakici = selin, Baslik = "Yürüyüş",              Tur = HatirlatmaTuru.Egzersiz, Saat = new TimeOnly(17, 0),  Tekrar = TekrarTipi.Gunluk,   BaslangicTarihi = basla },
        };

        // --- Aktivite kayıtları: son 3 günün ilaç onayları + günlük durumlar ---
        var aktiviteler = new List<AktiviteKaydi>();
        var ruhHalleri = new[] { RuhHali.Iyi, RuhHali.Normal, RuhHali.CokIyi };
        for (var gun = 3; gun >= 1; gun--)
        {
            var tarih = DateTime.Today.AddDays(-gun);
            foreach (var h in hatirlatmalar.Where(h => h.Tur == HatirlatmaTuru.Ilac && h.AktifMi && h.Tekrar == TekrarTipi.Gunluk))
            {
                // Hasan dün akşam ilacını atlamış: anomali senaryosu.
                var atlandi = h.Yasli == hasan && h.Baslik == "Donepezil" && gun == 1;
                aktiviteler.Add(new AktiviteKaydi
                {
                    Yasli = h.Yasli,
                    Hatirlatma = h,
                    Tur = AktiviteTuru.HatirlatmaOnayi,
                    Durum = atlandi ? AktiviteDurumu.Atlandi : AktiviteDurumu.Tamamlandi,
                    KayitZamani = tarih.Add(h.Saat.ToTimeSpan()).AddMinutes(atlandi ? 60 : 5),
                });
            }

            foreach (var (y, i) in new[] { ayse, mehmet, fatma, zeynep }.Select((y, i) => (y, i)))
            {
                aktiviteler.Add(new AktiviteKaydi
                {
                    Yasli = y,
                    Tur = AktiviteTuru.GunlukDurum,
                    RuhHali = ruhHalleri[(gun + i) % ruhHalleri.Length],
                    KayitZamani = tarih.AddHours(20),
                });
            }
        }

        // Mehmet dün kendini kötü hissettiğini bildirmiş.
        var mehmetKotu = new AktiviteKaydi
        {
            Yasli = mehmet,
            Tur = AktiviteTuru.GunlukDurum,
            RuhHali = RuhHali.Kotu,
            Not = "Nefes darlığı arttı.",
            KayitZamani = DateTime.Today.AddDays(-1).AddHours(14),
        };
        aktiviteler.Add(mehmetKotu);

        var atlananIlac = aktiviteler.Single(a => a.Durum == AktiviteDurumu.Atlandi);

        // --- Olaylar ---
        var olaylar = new List<Olay>
        {
            new() { Yasli = hasan,  AktiviteKaydi = atlananIlac, IlgilenenBakici = murat, Tur = OlayTuru.KacirilanHatirlatma, Seviye = OlaySeviyesi.Yuksek, Aciklama = "Donepezil dozu atlandı.", OlusmaZamani = atlananIlac.KayitZamani, Durum = OlayDurumu.Goruldu },
            new() { Yasli = mehmet, AktiviteKaydi = mehmetKotu,  IlgilenenBakici = null,  Tur = OlayTuru.Anomali,             Seviye = OlaySeviyesi.Orta,   Aciklama = "Ruh hali 'Kötü' bildirildi: nefes darlığı.", OlusmaZamani = mehmetKotu.KayitZamani, Durum = OlayDurumu.Acik },
            new() { Yasli = ayse,   AktiviteKaydi = null,        IlgilenenBakici = elif,  Tur = OlayTuru.AcilButon,           Seviye = OlaySeviyesi.Kritik, Aciklama = "Acil butona basıldı (düşme). Yakını aradı, sorun yok.", OlusmaZamani = DateTime.Today.AddDays(-5).AddHours(15), Durum = OlayDurumu.Cozuldu, CozulmeZamani = DateTime.Today.AddDays(-5).AddHours(15).AddMinutes(20) },
        };

        context.Yaslilar.AddRange(ayse, mehmet, fatma, hasan, zeynep);
        context.Bakicilar.AddRange(elif, can, selin, murat);
        context.YasliBakicilar.AddRange(eslesmeler);
        context.Hatirlatmalar.AddRange(hatirlatmalar);
        context.AktiviteKayitlari.AddRange(aktiviteler);
        context.Olaylar.AddRange(olaylar);

        // Tek SaveChanges = tek transaction: biri hata verirse hiçbiri eklenmez.
        await context.SaveChangesAsync();
    }
}
