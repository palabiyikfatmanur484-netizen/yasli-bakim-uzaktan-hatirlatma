using YasliBakim.Business.Abstract;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Concrete;

public class OlayManager : IOlayService
{
    private readonly IOlayRepository _repository;

    public OlayManager(IOlayRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Olay>> AcikOlaylarAsync() => _repository.GetAcikOlaylarAsync();

    public Task<Olay?> GetirAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<Olay> AcilButonAsync(int yasliId, string? aciklama = null)
    {
        var olay = new Olay
        {
            YasliId = yasliId,
            Tur = OlayTuru.AcilButon,
            Seviye = OlaySeviyesi.Kritik,
            Aciklama = aciklama ?? "Acil butona basıldı.",
        };
        await _repository.AddAsync(olay);
        await _repository.SaveAsync();
        return olay;
    }

    public async Task GorulduIsaretleAsync(int olayId, int bakiciId)
    {
        var olay = await OlayGetirAsync(olayId);
        if (olay.Durum == OlayDurumu.Cozuldu)
            throw new InvalidOperationException("Çözülmüş olay tekrar açılamaz.");

        olay.Durum = OlayDurumu.Goruldu;
        olay.IlgilenenBakiciId = bakiciId;
        await _repository.SaveAsync();
    }

    public async Task CozulduIsaretleAsync(int olayId)
    {
        var olay = await OlayGetirAsync(olayId);
        olay.Durum = OlayDurumu.Cozuldu;
        olay.CozulmeZamani = DateTime.Now;
        await _repository.SaveAsync();
    }

    public async Task SilAsync(int id)
    {
        _repository.Delete(await OlayGetirAsync(id));
        await _repository.SaveAsync();
    }

    private async Task<Olay> OlayGetirAsync(int id) =>
        await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Olay bulunamadı (Id={id}).");
}
