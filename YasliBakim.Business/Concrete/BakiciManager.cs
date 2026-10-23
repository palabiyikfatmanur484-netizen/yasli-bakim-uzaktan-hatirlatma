using YasliBakim.Business.Abstract;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Concrete;

public class BakiciManager : IBakiciService
{
    private readonly IBakiciRepository _repository;
    private readonly IGenericRepository<YasliBakici> _yasliBakiciRepository;
    private readonly IYasliRepository _yasliRepository;

    public BakiciManager(IBakiciRepository repository, IGenericRepository<YasliBakici> yasliBakiciRepository,
        IYasliRepository yasliRepository)
    {
        _repository = repository;
        _yasliBakiciRepository = yasliBakiciRepository;
        _yasliRepository = yasliRepository;
    }

    public async Task<List<Bakici>> TumunuGetirAsync() =>
        (await _repository.GetAllAsync()).OrderBy(b => b.Soyad).ThenBy(b => b.Ad).ToList();

    public Task<Bakici?> GetirAsync(int id) => _repository.GetByIdAsync(id);

    public Task<List<Yasli>> YaslilariGetirAsync(int bakiciId) => _repository.GetYaslilariAsync(bakiciId);

    public async Task EkleAsync(Bakici bakici)
    {
        bakici.Eposta = bakici.Eposta.Trim().ToLowerInvariant();
        // Veritabanındaki UX_Bakici_Eposta indeksi de engeller; burada anlaşılır bir mesaj veriyoruz.
        if (await _repository.AnyAsync(b => b.Eposta == bakici.Eposta))
            throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı.");

        await _repository.AddAsync(bakici);
        await _repository.SaveAsync();
    }

    public async Task GuncelleAsync(Bakici bakici)
    {
        bakici.Eposta = bakici.Eposta.Trim().ToLowerInvariant();
        if (await _repository.AnyAsync(b => b.Eposta == bakici.Eposta && b.Id != bakici.Id))
            throw new InvalidOperationException("Bu e-posta adresi başka bir bakıcıya ait.");

        _repository.Update(bakici);
        await _repository.SaveAsync();
    }

    // Eşleştirmeler silinir (CASCADE); oluşturduğu hatırlatma ve ilgilendiği olaylarda alan NULL olur (SET NULL).
    public async Task SilAsync(int id)
    {
        var bakici = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Bakıcı bulunamadı (Id={id}).");
        _repository.Delete(bakici);
        await _repository.SaveAsync();
    }

    public async Task YasliyaAtaAsync(int yasliId, int bakiciId, string? yakinlikDerecesi, bool birincilSorumluMu)
    {
        if (await _yasliRepository.GetByIdAsync(yasliId) is null)
            throw new KeyNotFoundException($"Yaşlı bulunamadı (Id={yasliId}).");
        if (await _repository.GetByIdAsync(bakiciId) is null)
            throw new KeyNotFoundException($"Bakıcı bulunamadı (Id={bakiciId}).");
        if (await _yasliBakiciRepository.GetByKeyAsync(yasliId, bakiciId) is not null)
            throw new InvalidOperationException("Bu bakıcı zaten bu yaşlıya atanmış.");

        await _yasliBakiciRepository.AddAsync(new YasliBakici
        {
            YasliId = yasliId,
            BakiciId = bakiciId,
            YakinlikDerecesi = yakinlikDerecesi,
            BirincilSorumluMu = birincilSorumluMu,
        });
        await _yasliBakiciRepository.SaveAsync();
    }

    public async Task AtamayiKaldirAsync(int yasliId, int bakiciId)
    {
        var atama = await _yasliBakiciRepository.GetByKeyAsync(yasliId, bakiciId)
            ?? throw new KeyNotFoundException("Böyle bir atama yok.");
        _yasliBakiciRepository.Delete(atama);
        await _yasliBakiciRepository.SaveAsync();
    }
}
