using YasliBakim.Business.Abstract;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Concrete;

public class AktiviteManager : IAktiviteService
{
    private readonly IAktiviteKaydiRepository _repository;
    private readonly IOlayRepository _olayRepository;

    public AktiviteManager(IAktiviteKaydiRepository repository, IOlayRepository olayRepository)
    {
        _repository = repository;
        _olayRepository = olayRepository;
    }

    public Task<List<AktiviteKaydi>> SonKayitlarAsync(int yasliId, int gun = 7) =>
        _repository.GetSonKayitlarAsync(yasliId, DateTime.Today.AddDays(-gun));

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
