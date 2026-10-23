using YasliBakim.Business.Abstract;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Concrete;

public class AktiviteManager : IAktiviteService
{
    private readonly IAktiviteKaydiRepository _repository;
    private readonly IHatirlatmaRepository _hatirlatmaRepository;
    private readonly IOlayRepository _olayRepository;

    public AktiviteManager(IAktiviteKaydiRepository repository, IHatirlatmaRepository hatirlatmaRepository,
        IOlayRepository olayRepository)
    {
        _repository = repository;
        _hatirlatmaRepository = hatirlatmaRepository;
        _olayRepository = olayRepository;
    }

    public Task<List<AktiviteKaydi>> SonKayitlarAsync(int yasliId, int gun = 7) =>
        _repository.GetSonKayitlarAsync(yasliId, DateTime.Today.AddDays(-gun));

    public async Task<AktiviteKaydi> HatirlatmaOnaylaAsync(int hatirlatmaId, AktiviteDurumu durum, string? not = null)
    {
        var hatirlatma = await _hatirlatmaRepository.GetByIdAsync(hatirlatmaId)
            ?? throw new KeyNotFoundException($"Hatırlatma bulunamadı (Id={hatirlatmaId}).");

        var kayit = new AktiviteKaydi
        {
            YasliId = hatirlatma.YasliId,
            HatirlatmaId = hatirlatma.Id,
            Tur = AktiviteTuru.HatirlatmaOnayi,
            Durum = durum,
            Not = not,
        };
        await _repository.AddAsync(kayit);

        // Atlanan hatırlatma yakınlara bildirilir.
        if (durum == AktiviteDurumu.Atlandi)
        {
            await _olayRepository.AddAsync(new Olay
            {
                YasliId = hatirlatma.YasliId,
                AktiviteKaydi = kayit,
                Tur = OlayTuru.KacirilanHatirlatma,
                Seviye = hatirlatma.Tur == HatirlatmaTuru.Ilac ? OlaySeviyesi.Yuksek : OlaySeviyesi.Dusuk,
                Aciklama = $"'{hatirlatma.Baslik}' atlandı.",
            });
        }

        // Repository'ler aynı DbContext'i paylaşır: kayıt ve olay tek SaveChanges ile tek transaction içinde yazılır.
        await _repository.SaveAsync();
        return kayit;
    }

    public async Task<AktiviteKaydi> GunlukDurumBildirAsync(int yasliId, RuhHali ruhHali, string? not = null)
    {
        var kayit = new AktiviteKaydi
        {
            YasliId = yasliId,
            Tur = AktiviteTuru.GunlukDurum,
            RuhHali = ruhHali,
            Not = not,
        };
        await _repository.AddAsync(kayit);

        if (ruhHali is RuhHali.Kotu or RuhHali.CokKotu)
        {
            await _olayRepository.AddAsync(new Olay
            {
                YasliId = yasliId,
                AktiviteKaydi = kayit,
                Tur = OlayTuru.Anomali,
                Seviye = ruhHali == RuhHali.CokKotu ? OlaySeviyesi.Yuksek : OlaySeviyesi.Orta,
                Aciklama = $"Ruh hali '{ruhHali}' bildirildi.",
            });
        }

        await _repository.SaveAsync();
        return kayit;
    }
}
