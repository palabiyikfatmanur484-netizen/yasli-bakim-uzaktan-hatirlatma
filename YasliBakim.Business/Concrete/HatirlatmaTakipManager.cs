using YasliBakim.Business.Abstract;
using YasliBakim.Business.Zamanlama;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Concrete;

public class HatirlatmaTakipManager : IHatirlatmaTakipService
{
    private readonly IHatirlatmaRepository _hatirlatmaRepository;
    private readonly IAktiviteKaydiRepository _aktiviteRepository;
    private readonly IOlayRepository _olayRepository;
    // Saat dışarıdan verilir: testlerde "şu an 09:10" gibi sabit bir zaman kullanılabilir.
    private readonly TimeProvider _saat;

    public HatirlatmaTakipManager(IHatirlatmaRepository hatirlatmaRepository, IAktiviteKaydiRepository aktiviteRepository,
        IOlayRepository olayRepository, TimeProvider saat)
    {
        _hatirlatmaRepository = hatirlatmaRepository;
        _aktiviteRepository = aktiviteRepository;
        _olayRepository = olayRepository;
        _saat = saat;
    }

    private DateTime Simdi => _saat.GetLocalNow().DateTime;

    public async Task<List<PlanOgesi>> GunlukPlanAsync(int yasliId, DateOnly tarih)
    {
        var hatirlatmalar = (await _hatirlatmaRepository.GetGecerliHatirlatmalarAsync(yasliId, tarih))
            .Where(h => HatirlatmaZamanlayici.GerceklesirMi(h, tarih))
            .ToList();
        if (hatirlatmalar.Count == 0)
            return new();

        var gunBasi = tarih.ToDateTime(TimeOnly.MinValue);
        var kayitlar = (await _aktiviteRepository.GetOnaylarAsync(hatirlatmalar.Select(h => h.Id).ToList(), gunBasi, gunBasi.AddDays(1)))
            .ToDictionary(a => (a.HatirlatmaId!.Value, a.PlanlananZaman!.Value));

        var simdi = Simdi;
        return hatirlatmalar
            .Select(h =>
            {
                var planlanan = HatirlatmaZamanlayici.PlanlananZaman(h, tarih);
                kayitlar.TryGetValue((h.Id, planlanan), out var kayit);
                return new PlanOgesi(h, planlanan, DurumHesapla(planlanan, kayit, simdi), kayit);
            })
            .OrderBy(p => p.PlanlananZaman)
            .ToList();
    }

    private static PlanDurumu DurumHesapla(DateTime planlanan, AktiviteKaydi? kayit, DateTime simdi) =>
        kayit?.Durum switch
        {
            AktiviteDurumu.Tamamlandi => PlanDurumu.Tamamlandi,
            AktiviteDurumu.Gecikti => PlanDurumu.Gecikti,
            AktiviteDurumu.Atlandi => PlanDurumu.Atlandi,
            AktiviteDurumu.Kacirildi => PlanDurumu.Kacirildi,
            // Kayıt yoksa durum saatten çıkarılır.
            _ when HatirlatmaKurallari.KacirildiMi(planlanan, simdi) => PlanDurumu.Kacirildi,
            _ when HatirlatmaKurallari.OnayPenceresiAcikMi(planlanan, simdi) => PlanDurumu.OnayBekleniyor,
            _ => PlanDurumu.Bekliyor,
        };

    public async Task<AktiviteKaydi> YanitlaAsync(int yasliId, int hatirlatmaId, DateTime planlananZaman, bool alindi, string? not = null)
    {
        var h = await _hatirlatmaRepository.GetByIdAsync(hatirlatmaId)
            ?? throw new KeyNotFoundException($"Hatırlatma bulunamadı (Id={hatirlatmaId}).");

        // Sahiplik: yaşlı yalnızca kendi hatırlatmasını yanıtlayabilir.
        if (h.YasliId != yasliId)
            throw new UnauthorizedAccessException("Bu hatırlatma size ait değil.");

        // Gönderilen zaman gerçekten bu hatırlatmanın bir tekrarı mı? (uydurma bir saat gönderilemez)
        var tarih = DateOnly.FromDateTime(planlananZaman);
        if (!HatirlatmaZamanlayici.GerceklesirMi(h, tarih) || HatirlatmaZamanlayici.PlanlananZaman(h, tarih) != planlananZaman)
            throw new ArgumentException("Bu saatte planlanmış bir hatırlatma yok.");

        var simdi = Simdi;
        if (simdi < planlananZaman - HatirlatmaKurallari.ErkenOnaySuresi)
            throw new InvalidOperationException("Bu hatırlatmanın onay zamanı henüz gelmedi.");
        if (HatirlatmaKurallari.KacirildiMi(planlananZaman, simdi))
            throw new InvalidOperationException("Onay süresi doldu; bu hatırlatma kaçırıldı olarak kaydedilir.");

        var mevcut = await _aktiviteRepository.GetOnaylarAsync([h.Id], planlananZaman, planlananZaman.AddTicks(1));
        if (mevcut.Count > 0)
            throw new InvalidOperationException("Bu hatırlatma zaten yanıtlandı.");

        var kayit = new AktiviteKaydi
        {
            YasliId = h.YasliId,
            HatirlatmaId = h.Id,
            Tur = AktiviteTuru.HatirlatmaOnayi,
            PlanlananZaman = planlananZaman,
            KayitZamani = simdi,
            Durum = alindi ? HatirlatmaKurallari.AlindiDurumu(planlananZaman, simdi) : AktiviteDurumu.Atlandi,
            Not = not,
        };
        await _aktiviteRepository.AddAsync(kayit);

        if (kayit.Durum == AktiviteDurumu.Atlandi && HatirlatmaKurallari.BildirimGerektirir(h.Tur))
            await AlarmEkleAsync(h, kayit, $"'{h.Baslik}' ({planlananZaman:HH:mm}) atlandı.");

        // Kayıt ve olay tek SaveChanges ile (tek transaction) yazılır.
        await _aktiviteRepository.SaveAsync();
        return kayit;
    }

    public async Task<int> KacirilanlariIsleAsync()
    {
        var simdi = Simdi;
        // Gece yarısından hemen sonra dünün geç saatli dozları da kontrol edilsin diye dün + bugün.
        var bugun = DateOnly.FromDateTime(simdi);
        var dun = bugun.AddDays(-1);

        var hatirlatmalar = await _hatirlatmaRepository.GetAktifHatirlatmalarAsync(dun, bugun);
        var adaylar = hatirlatmalar
            .SelectMany(h => HatirlatmaZamanlayici.Tekrarlar(h, dun, bugun).Select(t => (Hatirlatma: h, Planlanan: t)))
            // Hatırlatma oluşturulmadan önceki tekrarlar kaçırılmış sayılmaz.
            .Where(x => HatirlatmaKurallari.KacirildiMi(x.Planlanan, simdi) && x.Planlanan >= x.Hatirlatma.OlusturmaTarihi)
            .ToList();
        if (adaylar.Count == 0)
            return 0;

        var yanitlanmis = (await _aktiviteRepository.GetOnaylarAsync(
                adaylar.Select(x => x.Hatirlatma.Id).Distinct().ToList(),
                dun.ToDateTime(TimeOnly.MinValue), bugun.AddDays(1).ToDateTime(TimeOnly.MinValue)))
            .Select(a => (a.HatirlatmaId!.Value, a.PlanlananZaman!.Value))
            .ToHashSet();

        var sayi = 0;
        foreach (var (h, planlanan) in adaylar.Where(x => !yanitlanmis.Contains((x.Hatirlatma.Id, x.Planlanan))))
        {
            var kayit = new AktiviteKaydi
            {
                YasliId = h.YasliId,
                HatirlatmaId = h.Id,
                Tur = AktiviteTuru.HatirlatmaOnayi,
                PlanlananZaman = planlanan,
                KayitZamani = simdi,
                Durum = AktiviteDurumu.Kacirildi,
                Not = "Süresi içinde yanıt verilmedi (otomatik).",
            };
            await _aktiviteRepository.AddAsync(kayit);

            if (HatirlatmaKurallari.BildirimGerektirir(h.Tur))
                await AlarmEkleAsync(h, kayit, $"'{h.Baslik}' ({planlanan:dd.MM HH:mm}) onaylanmadı.");
            sayi++;
        }

        await _aktiviteRepository.SaveAsync();
        return sayi;
    }

    public async Task<UyumOzeti> UyumOzetiAsync(int yasliId, int gun = 7)
    {
        var simdi = Simdi;
        var kayitlar = await _aktiviteRepository.GetYaslininOnaylariAsync(yasliId, simdi.Date.AddDays(-gun), simdi);
        return new UyumOzeti(
            Toplam: kayitlar.Count,
            Zamaninda: kayitlar.Count(a => a.Durum == AktiviteDurumu.Tamamlandi),
            Gec: kayitlar.Count(a => a.Durum == AktiviteDurumu.Gecikti),
            Atlanan: kayitlar.Count(a => a.Durum == AktiviteDurumu.Atlandi),
            Kacirilan: kayitlar.Count(a => a.Durum == AktiviteDurumu.Kacirildi));
    }

    private Task AlarmEkleAsync(Hatirlatma h, AktiviteKaydi kayit, string aciklama) =>
        _olayRepository.AddAsync(new Olay
        {
            YasliId = h.YasliId,
            AktiviteKaydi = kayit,
            Tur = OlayTuru.KacirilanHatirlatma,
            Seviye = HatirlatmaKurallari.AlarmSeviyesi(h.Tur),
            Aciklama = aciklama,
            OlusmaZamani = Simdi,
        });
}
