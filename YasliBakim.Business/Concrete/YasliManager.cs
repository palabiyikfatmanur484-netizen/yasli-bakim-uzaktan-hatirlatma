using YasliBakim.Business.Abstract;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.Business.Concrete;

public class YasliManager : IYasliService
{
    private readonly IGenericRepository<Yasli> _repository;

    public YasliManager(IGenericRepository<Yasli> repository)
    {
        _repository = repository;
    }

    public Task<List<Yasli>> TumunuGetirAsync() => _repository.GetAllAsync();

    public Task<Yasli?> GetirAsync(int id) => _repository.GetByIdAsync(id);

    public async Task EkleAsync(Yasli yasli)
    {
        // İş kuralı örneği: doğum tarihi gelecekte olamaz.
        if (yasli.DogumTarihi > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Doğum tarihi gelecekte olamaz.");

        await _repository.AddAsync(yasli);
        await _repository.SaveAsync();
    }
}
