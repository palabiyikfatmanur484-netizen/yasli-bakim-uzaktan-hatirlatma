using YasliBakim.Business.Abstract;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Concrete;

public class YasliManager : IYasliService
{
    private readonly IYasliRepository _repository;

    public YasliManager(IYasliRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Yasli>> TumunuGetirAsync() =>
        (await _repository.GetAllAsync()).OrderBy(y => y.Soyad).ThenBy(y => y.Ad).ToList();

    public Task<Yasli?> GetirAsync(int id) => _repository.GetByIdAsync(id);

    public Task<Yasli?> DetayGetirAsync(int id) => _repository.GetDetayAsync(id);

    public Task<List<Yasli>> AraAsync(string metin) => _repository.AraAsync(metin.Trim());

    public async Task EkleAsync(Yasli yasli)
    {
        Dogrula(yasli);
        await _repository.AddAsync(yasli);
        await _repository.SaveAsync();
    }

    public async Task GuncelleAsync(Yasli yasli)
    {
        Dogrula(yasli);
        _repository.Update(yasli);
        await _repository.SaveAsync();
    }

    // Hatırlatma, aktivite ve olay kayıtları veritabanında ON DELETE CASCADE ile silinir.
    public async Task SilAsync(int id)
    {
        var yasli = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Yaşlı bulunamadı (Id={id}).");
        _repository.Delete(yasli);
        await _repository.SaveAsync();
    }

    private static void Dogrula(Yasli yasli)
    {
        if (string.IsNullOrWhiteSpace(yasli.Ad) || string.IsNullOrWhiteSpace(yasli.Soyad))
            throw new ArgumentException("Ad ve soyad boş olamaz.");
        if (yasli.DogumTarihi > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Doğum tarihi gelecekte olamaz.");
    }
}
