using YasliBakim.Business.Abstract;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Concrete;

public class HatirlatmaManager : IHatirlatmaService
{
    private readonly IHatirlatmaRepository _repository;
    private readonly IAktiviteKaydiRepository _aktiviteRepository;

    public HatirlatmaManager(IHatirlatmaRepository repository, IAktiviteKaydiRepository aktiviteRepository)
    {
        _repository = repository;
        _aktiviteRepository = aktiviteRepository;
    }

    public async Task<List<Hatirlatma>> YaslininHatirlatmalariAsync(int yasliId) =>
        (await _repository.GetAllAsync(h => h.YasliId == yasliId)).OrderBy(h => h.Saat).ToList();

    public Task<List<Hatirlatma>> BugunkuHatirlatmalarAsync(int yasliId) =>
        _repository.GetGecerliHatirlatmalarAsync(yasliId, DateOnly.FromDateTime(DateTime.Today));

    public Task<Hatirlatma?> GetirAsync(int id) => _repository.GetByIdAsync(id);

    public async Task EkleAsync(Hatirlatma hatirlatma)
    {
        Dogrula(hatirlatma);
        await _repository.AddAsync(hatirlatma);
        await _repository.SaveAsync();
    }

    public async Task GuncelleAsync(Hatirlatma hatirlatma)
    {
        Dogrula(hatirlatma);
        _repository.Update(hatirlatma);
        await _repository.SaveAsync();
    }

    public async Task PasifeAlAsync(int id)
    {
        var hatirlatma = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Hatırlatma bulunamadı (Id={id}).");
        hatirlatma.AktifMi = false;
        await _repository.SaveAsync();
    }

    // Onay geçmişi olan hatırlatma silinmez (FK: NO ACTION), pasife alınmalıdır.
    public async Task SilAsync(int id)
    {
        var hatirlatma = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Hatırlatma bulunamadı (Id={id}).");
        if (await _aktiviteRepository.AnyAsync(a => a.HatirlatmaId == id))
            throw new InvalidOperationException("Bu hatırlatmanın onay geçmişi var; silmek yerine pasife alın.");

        _repository.Delete(hatirlatma);
        await _repository.SaveAsync();
    }

    private static void Dogrula(Hatirlatma h)
    {
        if (string.IsNullOrWhiteSpace(h.Baslik))
            throw new ArgumentException("Başlık boş olamaz.");
        if (h.BitisTarihi is not null && h.BitisTarihi < h.BaslangicTarihi)
            throw new ArgumentException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
    }
}
