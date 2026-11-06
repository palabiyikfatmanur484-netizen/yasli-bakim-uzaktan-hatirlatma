using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Business.Kurallar;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages.Aktiviteler;

// Tüm roller açabilir; kimin hangi yaşlıyı göreceğini HedefYasliAsync belirler (yaşlı: yalnızca kendisi).
public class GecmisModel : PageModel
{
    private readonly IAktiviteService _aktiviteService;
    private readonly IYasliService _yasliService;
    private readonly IBakiciService _bakiciService;
    private readonly TimeProvider _saat;

    public GecmisModel(IAktiviteService aktiviteService, IYasliService yasliService, IBakiciService bakiciService, TimeProvider saat)
    {
        _aktiviteService = aktiviteService;
        _yasliService = yasliService;
        _bakiciService = bakiciService;
        _saat = saat;
    }

    [BindProperty(SupportsGet = true)]
    public int? YasliId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Baslangic { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Bitis { get; set; }

    [BindProperty(SupportsGet = true)]
    public AktiviteTuru? Tur { get; set; }

    public Yasli Yasli { get; private set; } = null!;
    public List<GunOzeti> Ozet { get; private set; } = new();
    public List<AktiviteKaydi> Kayitlar { get; private set; } = new();
    public DateTime Simdi => _saat.GetLocalNow().DateTime;
    public string? Hata { get; private set; }

    [TempData]
    public string? Mesaj { get; set; }

    public bool YasliMi => User.IsInRole(Roller.Yasli);

    // Ekranda "Sil" butonu yalnızca servisin de izin vereceği kayıtlarda görünür. Yönetici kayıt girmediği için silemez.
    public bool SilinebilirMi(AktiviteKaydi k) =>
        !User.IsInRole(Roller.Yonetici) && AktiviteKurallari.SilinebilirMi(k, User.BakiciId(), Simdi);

    public async Task<IActionResult> OnGetAsync()
    {
        var sonuc = await YukleAsync();
        if (sonuc is not null)
            return sonuc;

        var bugun = DateOnly.FromDateTime(Simdi);
        Bitis ??= bugun;
        Baslangic ??= Bitis.Value.AddDays(-6);
        try
        {
            Ozet = await _aktiviteService.GunlukOzetAsync(Yasli.Id, Baslangic.Value, Bitis.Value);
            Kayitlar = await _aktiviteService.GecmisAsync(Yasli.Id, Baslangic.Value, Bitis.Value, Tur);
        }
        catch (ArgumentException ex)
        {
            // Geçersiz aralıkta hata gösterilir ve varsayılan son 7 güne dönülür.
            Hata = ex.Message;
            Bitis = bugun;
            Baslangic = bugun.AddDays(-6);
            Tur = null;
            Ozet = await _aktiviteService.GunlukOzetAsync(Yasli.Id, Baslangic.Value, Bitis.Value);
            Kayitlar = await _aktiviteService.GecmisAsync(Yasli.Id, Baslangic.Value, Bitis.Value);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSilAsync(int kayitId)
    {
        if (User.IsInRole(Roller.Yonetici))
            return Forbid();
        var sonuc = await YukleAsync();
        if (sonuc is not null)
            return sonuc;

        try
        {
            // Yaşlı ve bakıcı kimliği oturumdan gelir; servis kaydın bu yaşlıya ait ve bu kişi tarafından girildiğini kontrol eder.
            await _aktiviteService.AktiviteSilAsync(kayitId, Yasli.Id, User.BakiciId());
            Mesaj = "Kayıt silindi.";
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            Mesaj = ex.Message;
        }
        return RedirectToPage(new { yasliId = YasliMi ? (int?)null : Yasli.Id, Baslangic, Bitis, Tur });
    }

    private async Task<IActionResult?> YukleAsync()
    {
        var yasliId = await User.HedefYasliAsync(YasliId, _bakiciService);
        if (yasliId is null)
            return Forbid();
        var yasli = await _yasliService.GetirAsync(yasliId.Value);
        if (yasli is null)
            return NotFound();
        Yasli = yasli;
        return null;
    }
}
