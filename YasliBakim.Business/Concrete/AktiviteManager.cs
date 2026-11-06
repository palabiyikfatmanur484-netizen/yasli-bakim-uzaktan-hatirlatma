using YasliBakim.Business.Abstract;
using YasliBakim.Business.Kurallar;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Concrete;

public class AktiviteManager : IAktiviteService
{
    private readonly IAktiviteKaydiRepository _repository;
    private readonly IOlayRepository _olayRepository;
    private readonly IYasliRepository _yasliRepository;
    private readonly TimeProvider _saat;

    public AktiviteManager(IAktiviteKaydiRepository repository, IOlayRepository olayRepository,
        IYasliRepository yasliRepository, TimeProvider saat)
    {
        _repository = repository;
        _olayRepository = olayRepository;
        _yasliRepository = yasliRepository;
        _saat = saat;
    }

    private DateTime Simdi => _saat.GetLocalNow().DateTime;
    private DateOnly Bugun => DateOnly.FromDateTime(Simdi);

    public Task<AktiviteKaydi?> BugunkuDurumAsync(int yasliId) => _repository.GetGunlukDurumAsync(yasliId, Bugun);

    public async Task<AktiviteKaydi> GunlukDurumBildirAsync(int yasliId, RuhHali ruhHali, int? agriSeviyesi = null, string? not = null)
    {
        if (!Enum.IsDefined(ruhHali))
            throw new ArgumentException("Geçersiz ruh hali.");
        if (agriSeviyesi is < 0 or > AktiviteKurallari.EnYuksekAgri)
            throw new ArgumentException($"Ağrı seviyesi 0 ile {AktiviteKurallari.EnYuksekAgri} arasında olmalı.");
        not = NotuNormallestir(not);

        var simdi = Simdi;
        var kayit = await _repository.GetGunlukDurumAsync(yasliId, Bugun);
        var yeniMi = kayit is null;
        if (kayit is null)
        {
            kayit = new AktiviteKaydi { YasliId = yasliId, Tur = AktiviteTuru.GunlukDurum, OlusturmaTarihi = simdi };
            await _repository.AddAsync(kayit);
        }
        // Aynı gün ikinci bildirim yeni satır açmaz, günün durumunu günceller (bakıcı günün son hâlini görür).
        kayit.RuhHali = ruhHali;
        kayit.AgriSeviyesi = agriSeviyesi;
        kayit.Not = not;
        kayit.KayitZamani = simdi;

        // Bir günlük durumdan en fazla bir olay doğar: gün içinde "kötü"yü iki kez bildirmek yakınlara iki alarm göndermez.
        // Sonradan "iyi"ye dönülse de önceki olay silinmez; bakıcı görüp kapatır.
        var seviye = AktiviteKurallari.AnomaliSeviyesi(ruhHali, agriSeviyesi);
        if (seviye is not null && (yeniMi || !await _olayRepository.AnyAsync(o => o.AktiviteKaydiId == kayit.Id)))
        {
            await _olayRepository.AddAsync(new Olay
            {
                YasliId = yasliId,
                AktiviteKaydi = kayit,
                Tur = OlayTuru.Anomali,
                Seviye = seviye.Value,
                Aciklama = AnomaliAciklamasi(ruhHali, agriSeviyesi, not),
                OlusmaZamani = simdi,
            });
        }

        // Kayıt ve olay tek SaveChanges ile (tek transaction) yazılır.
        await _repository.SaveAsync();
        return kayit;
    }

    public async Task<AktiviteKaydi> AktiviteEkleAsync(AktiviteGirdisi girdi)
    {
        if (await _yasliRepository.GetByIdAsync(girdi.YasliId) is null)
            throw new KeyNotFoundException($"Yaşlı bulunamadı (Id={girdi.YasliId}).");
        if (!Enum.IsDefined(girdi.Kategori))
            throw new ArgumentException("Geçersiz aktivite türü.");
        if (girdi.SureDakika is < AktiviteKurallari.EnKisaSure or > AktiviteKurallari.EnUzunSure)
            throw new ArgumentException($"Süre {AktiviteKurallari.EnKisaSure} ile {AktiviteKurallari.EnUzunSure} dakika arasında olmalı.");

        var simdi = Simdi;
        // Saniyeler atılır: aynı dakikadaki çift gönderim aşağıda yakalanabilsin.
        var zaman = new DateTime(girdi.Zaman.Year, girdi.Zaman.Month, girdi.Zaman.Day, girdi.Zaman.Hour, girdi.Zaman.Minute, 0);
        if (zaman > simdi + AktiviteKurallari.IleriTolerans)
            throw new ArgumentException("Henüz yapılmamış bir aktivite kaydedilemez.");
        if (zaman < simdi - AktiviteKurallari.GeriyeDonukSinir)
            throw new ArgumentException("En fazla 2 gün öncesine ait aktivite kaydedilebilir.");
        var not = NotuNormallestir(girdi.Not);

        // Butona iki kez basılması (yaşlı kullanıcılarda sık) aynı aktiviteyi iki kez yazmasın.
        if (await _repository.AnyAsync(a => a.YasliId == girdi.YasliId && a.Tur == AktiviteTuru.Aktivite
                                            && a.Kategori == girdi.Kategori && a.KayitZamani == zaman))
            throw new InvalidOperationException("Bu aktivite bu saatte zaten kaydedilmiş.");

        var kayit = new AktiviteKaydi
        {
            YasliId = girdi.YasliId,
            KaydedenBakiciId = girdi.KaydedenBakiciId,
            Tur = AktiviteTuru.Aktivite,
            Kategori = girdi.Kategori,
            SureDakika = girdi.SureDakika,
            KayitZamani = zaman,
            OlusturmaTarihi = simdi,
            Not = not,
        };
        await _repository.AddAsync(kayit);
        await _repository.SaveAsync();
        return kayit;
    }

    public async Task AktiviteSilAsync(int kayitId, int yasliId, int? bakiciId)
    {
        var kayit = await _repository.GetByIdAsync(kayitId)
            ?? throw new KeyNotFoundException($"Kayıt bulunamadı (Id={kayitId}).");

        // Sahiplik: önce kayıt bu yaşlının mı, sonra silmek isteyen onu giren kişi mi?
        if (kayit.YasliId != yasliId)
            throw new UnauthorizedAccessException("Bu kayıt size ait değil.");
        if (kayit.Tur != AktiviteTuru.Aktivite)
            throw new InvalidOperationException("Hatırlatma onayları ve günlük durum kayıtları silinemez.");
        if (kayit.KaydedenBakiciId != bakiciId)
            throw new UnauthorizedAccessException("Kaydı yalnızca onu giren kişi silebilir.");
        if (!AktiviteKurallari.SilinebilirMi(kayit, bakiciId, Simdi))
            throw new InvalidOperationException("Kayıt 24 saatten eski olduğu için silinemez.");

        _repository.Delete(kayit);
        await _repository.SaveAsync();
    }

    public Task<List<AktiviteKaydi>> GecmisAsync(int yasliId, DateOnly baslangic, DateOnly bitis, AktiviteTuru? tur = null)
    {
        AraligiDogrula(baslangic, bitis);
        if (tur is not null && !Enum.IsDefined(tur.Value))
            throw new ArgumentException("Geçersiz kayıt türü.");
        return _repository.GetGecmisAsync(yasliId, baslangic.ToDateTime(TimeOnly.MinValue),
            bitis.AddDays(1).ToDateTime(TimeOnly.MinValue), tur);
    }

    public async Task<List<GunOzeti>> GunlukOzetAsync(int yasliId, DateOnly baslangic, DateOnly bitis)
    {
        var kayitlar = await GecmisAsync(yasliId, baslangic, bitis);

        // Onay kaydı ait olduğu dozun gününe sayılır (23:00 dozu ertesi gün 01:00'de "kaçırıldı" olsa bile).
        var gunlere = kayitlar.ToLookup(a => DateOnly.FromDateTime(a.PlanlananZaman ?? a.KayitZamani));

        var ozet = new List<GunOzeti>();
        for (var gun = bitis; gun >= baslangic; gun = gun.AddDays(-1))
        {
            var g = gunlere[gun].ToList();
            var onaylar = g.Where(a => a.Tur == AktiviteTuru.HatirlatmaOnayi).ToList();
            var aktiviteler = g.Where(a => a.Tur == AktiviteTuru.Aktivite).ToList();
            ozet.Add(new GunOzeti(
                Gun: gun,
                Durum: g.FirstOrDefault(a => a.Tur == AktiviteTuru.GunlukDurum),     // en yeni önce sıralı
                AktiviteSayisi: aktiviteler.Count,
                AktiviteDakika: aktiviteler.Sum(a => a.SureDakika ?? 0),
                AlinanDoz: onaylar.Count(a => a.Durum is AktiviteDurumu.Tamamlandi or AktiviteDurumu.Gecikti),
                YanitlananDoz: onaylar.Count));
        }
        return ozet;
    }

    private static void AraligiDogrula(DateOnly baslangic, DateOnly bitis)
    {
        if (bitis < baslangic)
            throw new ArgumentException("Bitiş tarihi başlangıçtan önce olamaz.");
        if (bitis.DayNumber - baslangic.DayNumber + 1 > AktiviteKurallari.GecmisEnFazlaGun)
            throw new ArgumentException($"En fazla {AktiviteKurallari.GecmisEnFazlaGun} günlük geçmiş gösterilebilir.");
    }

    private static string? NotuNormallestir(string? not)
    {
        if (string.IsNullOrWhiteSpace(not))
            return null;
        not = not.Trim();
        if (not.Length > AktiviteKurallari.NotUzunlugu)
            throw new ArgumentException($"Not en fazla {AktiviteKurallari.NotUzunlugu} karakter olabilir.");
        return not;
    }

    private static string AnomaliAciklamasi(RuhHali ruhHali, int? agri, string? not)
    {
        var metin = $"Günlük durum: ruh hali '{ruhHali.GorunenAd()}'";
        if (agri is not null)
            metin += $", ağrı {agri}/10";
        if (not is not null)
            metin += $". Not: {not}";
        // Olay.Aciklama 500 karakterle sınırlı.
        return metin.Length > 500 ? metin[..497] + "..." : metin;
    }
}
