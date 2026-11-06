using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages;

// Yaşlının kendi ekranı.
[Authorize(Roles = Roller.Yasli)]
public class PanelimModel : PageModel
{
    private readonly IYasliService _yasliService;
    private readonly IHatirlatmaTakipService _takipService;
    private readonly IAktiviteService _aktiviteService;
    private readonly IOlayService _olayService;
    private readonly TimeProvider _saat;

    public PanelimModel(IYasliService yasliService, IHatirlatmaTakipService takipService,
        IAktiviteService aktiviteService, IOlayService olayService, TimeProvider saat)
    {
        _yasliService = yasliService;
        _takipService = takipService;
        _aktiviteService = aktiviteService;
        _olayService = olayService;
        _saat = saat;
    }

    public Yasli Yasli { get; private set; } = null!;
    public List<PlanOgesi> Bugun { get; private set; } = new();
    public AktiviteKaydi? BugunkuDurum { get; private set; }

    [TempData]
    public string? Mesaj { get; set; }

    [TempData]
    public string? Hata { get; set; }

    public async Task OnGetAsync()
    {
        var yasliId = User.YasliId()!.Value;
        Yasli = (await _yasliService.GetirAsync(yasliId))!;
        Bugun = await _takipService.GunlukPlanAsync(yasliId, DateOnly.FromDateTime(_saat.GetLocalNow().DateTime));
        BugunkuDurum = await _aktiviteService.BugunkuDurumAsync(yasliId);
    }

    public async Task<IActionResult> OnPostYanitAsync(int hatirlatmaId, DateTime planlanan, bool alindi)
    {
        try
        {
            // Yaşlı kimliği formdan değil oturumdan alınır; servis hatırlatmanın bu yaşlıya ait olduğunu kontrol eder.
            await _takipService.YanitlaAsync(User.YasliId()!.Value, hatirlatmaId, planlanan, alindi);
            Mesaj = alindi ? "Kaydedildi. Teşekkürler!" : "Atlandı olarak kaydedildi.";
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            Hata = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDurumAsync(RuhHali? ruhHali, int? agri, string? not)
    {
        if (ruhHali is null)
        {
            Hata = "Lütfen bugün nasıl hissettiğinizi seçin.";
            return RedirectToPage();
        }
        try
        {
            // Durum her zaman oturumdaki yaşlıya yazılır; formdan yaşlı kimliği alınmaz.
            await _aktiviteService.GunlukDurumBildirAsync(User.YasliId()!.Value, ruhHali.Value, agri, not);
            Mesaj = "Bugünkü durumunuz kaydedildi.";
        }
        catch (ArgumentException ex)
        {
            Hata = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAcilAsync()
    {
        // Yaşlı kimliği formdan değil oturumdan alınır: başkası adına alarm üretilemez.
        await _olayService.AcilButonAsync(User.YasliId()!.Value);
        Mesaj = "Yakınlarınıza haber verildi.";
        return RedirectToPage();
    }
}
