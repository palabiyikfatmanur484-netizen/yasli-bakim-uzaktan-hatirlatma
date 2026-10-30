using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages.Hatirlatmalar;

[Authorize(Roles = Roller.YoneticiVeyaBakici)]
public class DuzenleModel : PageModel
{
    private readonly IHatirlatmaService _hatirlatmaService;
    private readonly IYasliService _yasliService;
    private readonly IBakiciService _bakiciService;
    private readonly TimeProvider _saat;

    public DuzenleModel(IHatirlatmaService hatirlatmaService, IYasliService yasliService,
        IBakiciService bakiciService, TimeProvider saat)
    {
        _hatirlatmaService = hatirlatmaService;
        _yasliService = yasliService;
        _bakiciService = bakiciService;
        _saat = saat;
    }

    // Formdan yalnızca bu alanlar bağlanır; YasliId, AktifMi gibi alanlar formdan alınmaz.
    public class Girdi
    {
        [Required(ErrorMessage = "Başlık zorunlu."), StringLength(100, ErrorMessage = "En fazla 100 karakter.")]
        public string Baslik { get; set; } = "";

        [StringLength(500, ErrorMessage = "En fazla 500 karakter.")]
        public string? Aciklama { get; set; }

        public HatirlatmaTuru Tur { get; set; } = HatirlatmaTuru.Ilac;

        [Required(ErrorMessage = "Saat zorunlu.")]
        public TimeOnly Saat { get; set; } = new(9, 0);

        public TekrarTipi Tekrar { get; set; } = TekrarTipi.Gunluk;

        [Required(ErrorMessage = "Başlangıç tarihi zorunlu.")]
        public DateOnly BaslangicTarihi { get; set; }

        public DateOnly? BitisTarihi { get; set; }
    }

    [BindProperty]
    public Girdi Form { get; set; } = new();

    public Yasli Yasli { get; private set; } = null!;
    public bool YeniMi => Id is null;

    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? YasliId { get; set; }

    public string? Hata { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sonuc = await YukleAsync();
        if (sonuc is not null)
            return sonuc;

        if (YeniMi)
        {
            Form.BaslangicTarihi = DateOnly.FromDateTime(_saat.GetLocalNow().DateTime);
        }
        else
        {
            var h = (await _hatirlatmaService.GetirAsync(Id!.Value))!;
            Form = new Girdi
            {
                Baslik = h.Baslik, Aciklama = h.Aciklama, Tur = h.Tur, Saat = h.Saat, Tekrar = h.Tekrar,
                BaslangicTarihi = h.BaslangicTarihi, BitisTarihi = h.BitisTarihi,
            };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sonuc = await YukleAsync();
        if (sonuc is not null)
            return sonuc;
        if (!ModelState.IsValid)
            return Page();

        var hatirlatma = new Hatirlatma
        {
            Id = Id ?? 0,
            YasliId = Yasli.Id,
            OlusturanBakiciId = User.BakiciId(),
            Baslik = Form.Baslik,
            Aciklama = Form.Aciklama,
            Tur = Form.Tur,
            Saat = Form.Saat,
            Tekrar = Form.Tekrar,
            BaslangicTarihi = Form.BaslangicTarihi,
            BitisTarihi = Form.BitisTarihi,
        };

        try
        {
            if (YeniMi)
                await _hatirlatmaService.EkleAsync(hatirlatma);
            else
                await _hatirlatmaService.GuncelleAsync(hatirlatma);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            // İş kuralı hatası formda gösterilir, girilen değerler korunur.
            Hata = ex.Message;
            return Page();
        }

        TempData["Mesaj"] = YeniMi ? "Hatırlatma eklendi." : "Hatırlatma güncellendi.";
        return RedirectToPage("/Yaslilar/Detay", new { id = Yasli.Id });
    }

    // Hangi yaşlı için çalışıldığını bulur ve sahiplik kontrolü yapar. Sorun varsa hata sonucu döner.
    private async Task<IActionResult?> YukleAsync()
    {
        int yasliId;
        if (Id is not null)
        {
            var h = await _hatirlatmaService.GetirAsync(Id.Value);
            if (h is null)
                return NotFound();
            // Düzenlemede yaşlı her zaman kayıttan alınır; adres çubuğundaki yasliId'ye güvenilmez.
            yasliId = h.YasliId;
        }
        else if (YasliId is not null)
        {
            yasliId = YasliId.Value;
        }
        else
        {
            return BadRequest();
        }

        if (!await User.YasliyaErisebilirMiAsync(yasliId, _bakiciService))
            return Forbid();

        var yasli = await _yasliService.GetirAsync(yasliId);
        if (yasli is null)
            return NotFound();
        Yasli = yasli;
        return null;
    }
}
