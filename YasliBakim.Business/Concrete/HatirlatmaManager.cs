using YasliBakim.Business.Abstract;
using YasliBakim.Business.Zamanlama;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;

namespace YasliBakim.Business.Concrete;

public class HatirlatmaManager : IHatirlatmaService
{
    private readonly IHatirlatmaRepository _repository;
    private readonly IAktiviteKaydiRepository _aktiviteRepository;
    private readonly IYasliRepository _yasliRepository;
    private readonly TimeProvider _saat;

    public HatirlatmaManager(IHatirlatmaRepository repository, IAktiviteKaydiRepository aktiviteRepository,
        IYasliRepository yasliRepository, TimeProvider saat)
    {
        _repository = repository;
        _aktiviteRepository = aktiviteRepository;
        _yasliRepository = yasliRepository;
        _saat = saat;
    }

    private DateTime Simdi => _saat.GetLocalNow().DateTime;
    private DateOnly Bugun => DateOnly.FromDateTime(Simdi);

    public async Task<List<Hatirlatma>> YaslininHatirlatmalariAsync(int yasliId) =>
        (await _repository.GetAllAsync(h => h.YasliId == yasliId))
            .OrderByDescending(h => h.AktifMi).ThenBy(h => h.Saat).ToList();

    public async Task<List<Hatirlatma>> BugunkuHatirlatmalarAsync(int yasliId)
    {
        var bugun = Bugun;
        return (await _repository.GetGecerliHatirlatmalarAsync(yasliId, bugun))
            .Where(h => HatirlatmaZamanlayici.GerceklesirMi(h, bugun))
            .ToList();
    }

    public Task<Hatirlatma?> GetirAsync(int id) => _repository.GetByIdAsync(id);

    public async Task EkleAsync(Hatirlatma hatirlatma)
    {
        if (await _yasliRepository.GetByIdAsync(hatirlatma.YasliId) is null)
            throw new KeyNotFoundException($"Yaşlı bulunamadı (Id={hatirlatma.YasliId}).");
        if (hatirlatma.BaslangicTarihi < Bugun)
            throw new ArgumentException("Başlangıç tarihi geçmişte olamaz.");

        Normallestir(hatirlatma);
        await Dogrula(hatirlatma);

        hatirlatma.AktifMi = true;
        hatirlatma.OlusturmaTarihi = Simdi;
        await _repository.AddAsync(hatirlatma);
        await _repository.SaveAsync();
    }

    public async Task GuncelleAsync(Hatirlatma girdi)
    {
        var mevcut = await _repository.GetByIdAsync(girdi.Id)
            ?? throw new KeyNotFoundException($"Hatırlatma bulunamadı (Id={girdi.Id}).");

        // Başlangıç ancak değiştirildiyse bugünden önce olamaz (eski günlük hatırlatma düzenlenebilsin).
        if (girdi.BaslangicTarihi != mevcut.BaslangicTarihi && girdi.BaslangicTarihi < Bugun)
            throw new ArgumentException("Başlangıç tarihi geçmişte olamaz.");

        // Formdan gelen nesne doğrudan kaydedilmez (overposting): yalnızca izin verilen alanlar kopyalanır.
        // YasliId, OlusturanBakiciId, OlusturmaTarihi ve AktifMi değişmez.
        mevcut.Baslik = girdi.Baslik;
        mevcut.Aciklama = girdi.Aciklama;
        mevcut.Tur = girdi.Tur;
        mevcut.Saat = girdi.Saat;
        mevcut.Tekrar = girdi.Tekrar;
        mevcut.BaslangicTarihi = girdi.BaslangicTarihi;
        mevcut.BitisTarihi = girdi.BitisTarihi;

        Normallestir(mevcut);
        await Dogrula(mevcut);
        await _repository.SaveAsync();
    }

    public async Task PasifeAlAsync(int id)
    {
        var hatirlatma = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Hatırlatma bulunamadı (Id={id}).");
        hatirlatma.AktifMi = false;
        await _repository.SaveAsync();
    }

    public async Task AktifEtAsync(int id)
    {
        var hatirlatma = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Hatırlatma bulunamadı (Id={id}).");
        if (hatirlatma.BitisTarihi < Bugun)
            throw new InvalidOperationException("Süresi bitmiş hatırlatma aktif edilemez; bitiş tarihini güncelleyin.");
        if (await _repository.AyniHatirlatmaVarMiAsync(hatirlatma.YasliId, hatirlatma.Baslik, hatirlatma.Saat, hatirlatma.Id))
            throw new InvalidOperationException("Bu saatte aynı başlıklı aktif bir hatırlatma zaten var.");

        hatirlatma.AktifMi = true;
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

    private static void Normallestir(Hatirlatma h)
    {
        h.Baslik = (h.Baslik ?? "").Trim();
        h.Aciklama = string.IsNullOrWhiteSpace(h.Aciklama) ? null : h.Aciklama.Trim();
        // Tek seferlik hatırlatma yalnızca başlangıç gününde olur.
        if (h.Tekrar == TekrarTipi.Tek)
            h.BitisTarihi = h.BaslangicTarihi;
    }

    private async Task Dogrula(Hatirlatma h)
    {
        if (h.Baslik.Length == 0)
            throw new ArgumentException("Başlık boş olamaz.");
        if (h.Baslik.Length > 100)
            throw new ArgumentException("Başlık en fazla 100 karakter olabilir.");
        if (h.Aciklama?.Length > 500)
            throw new ArgumentException("Açıklama en fazla 500 karakter olabilir.");
        // Formdan uydurma bir sayı (ör. Tur=42) gelirse reddedilir.
        if (!Enum.IsDefined(h.Tur))
            throw new ArgumentException("Geçersiz hatırlatma türü.");
        if (!Enum.IsDefined(h.Tekrar))
            throw new ArgumentException("Geçersiz tekrar tipi.");
        if (h.BitisTarihi is not null && h.BitisTarihi < h.BaslangicTarihi)
            throw new ArgumentException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        if (await _repository.AyniHatirlatmaVarMiAsync(h.YasliId, h.Baslik, h.Saat, h.Id))
            throw new InvalidOperationException("Bu saatte aynı başlıklı aktif bir hatırlatma zaten var.");
    }
}
