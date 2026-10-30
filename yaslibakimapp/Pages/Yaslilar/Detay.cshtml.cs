using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Entities.Concrete;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages.Yaslilar;

[Authorize(Roles = Roller.YoneticiVeyaBakici)]
public class DetayModel : PageModel
{
    private readonly IYasliService _yasliService;
    private readonly IBakiciService _bakiciService;
    private readonly IAktiviteService _aktiviteService;
    private readonly IHatirlatmaService _hatirlatmaService;
    private readonly IHatirlatmaTakipService _takipService;
    private readonly TimeProvider _saat;

    public DetayModel(IYasliService yasliService, IBakiciService bakiciService, IAktiviteService aktiviteService,
        IHatirlatmaService hatirlatmaService, IHatirlatmaTakipService takipService, TimeProvider saat)
    {
        _yasliService = yasliService;
        _bakiciService = bakiciService;
        _aktiviteService = aktiviteService;
        _hatirlatmaService = hatirlatmaService;
        _takipService = takipService;
        _saat = saat;
    }

    public Yasli Yasli { get; private set; } = null!;
    public List<Hatirlatma> Hatirlatmalar { get; private set; } = new();
    public List<PlanOgesi> BugunkuPlan { get; private set; } = new();
    public UyumOzeti Uyum { get; private set; } = null!;
    public List<AktiviteKaydi> SonKayitlar { get; private set; } = new();
    public DateTime Simdi => _saat.GetLocalNow().DateTime;

    [TempData]
    public string? Mesaj { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Rol kontrolü yetmez: bakıcı, adres çubuğuna başka bir id yazarak (IDOR)
        // kendisine atanmamış yaşlının sağlık bilgilerini görememeli.
        if (!await User.YasliyaErisebilirMiAsync(id, _bakiciService))
            return Forbid();

        var yasli = await _yasliService.DetayGetirAsync(id);
        if (yasli is null)
            return NotFound();

        Yasli = yasli;
        Hatirlatmalar = await _hatirlatmaService.YaslininHatirlatmalariAsync(id);
        BugunkuPlan = await _takipService.GunlukPlanAsync(id, DateOnly.FromDateTime(Simdi));
        Uyum = await _takipService.UyumOzetiAsync(id, gun: 7);
        SonKayitlar = await _aktiviteService.SonKayitlarAsync(id, gun: 7);
        return Page();
    }

    public Task<IActionResult> OnPostPasifeAlAsync(int id, int hatirlatmaId) =>
        HatirlatmaIslemiAsync(id, hatirlatmaId, () => _hatirlatmaService.PasifeAlAsync(hatirlatmaId), "Hatırlatma pasife alındı.");

    public Task<IActionResult> OnPostAktifEtAsync(int id, int hatirlatmaId) =>
        HatirlatmaIslemiAsync(id, hatirlatmaId, () => _hatirlatmaService.AktifEtAsync(hatirlatmaId), "Hatırlatma tekrar aktif.");

    private async Task<IActionResult> HatirlatmaIslemiAsync(int yasliId, int hatirlatmaId, Func<Task> islem, string basariMesaji)
    {
        // Hem yaşlıya erişim hem de hatırlatmanın gerçekten o yaşlıya ait olduğu kontrol edilir.
        var hatirlatma = await _hatirlatmaService.GetirAsync(hatirlatmaId);
        if (hatirlatma is null || hatirlatma.YasliId != yasliId || !await User.YasliyaErisebilirMiAsync(yasliId, _bakiciService))
            return Forbid();

        try
        {
            await islem();
            Mesaj = basariMesaji;
        }
        catch (InvalidOperationException ex)
        {
            Mesaj = ex.Message;
        }
        return RedirectToPage(new { id = yasliId });
    }
}
