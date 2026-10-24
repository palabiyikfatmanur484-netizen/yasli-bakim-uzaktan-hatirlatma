using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Entities.Concrete;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages;

// Yaşlının kendi ekranı.
[Authorize(Roles = Roller.Yasli)]
public class PanelimModel : PageModel
{
    private readonly IYasliService _yasliService;
    private readonly IHatirlatmaService _hatirlatmaService;
    private readonly IOlayService _olayService;

    public PanelimModel(IYasliService yasliService, IHatirlatmaService hatirlatmaService, IOlayService olayService)
    {
        _yasliService = yasliService;
        _hatirlatmaService = hatirlatmaService;
        _olayService = olayService;
    }

    public Yasli Yasli { get; private set; } = null!;
    public List<Hatirlatma> Bugun { get; private set; } = new();

    [TempData]
    public string? Mesaj { get; set; }

    public async Task OnGetAsync()
    {
        var yasliId = User.YasliId()!.Value;
        Yasli = (await _yasliService.GetirAsync(yasliId))!;
        Bugun = await _hatirlatmaService.BugunkuHatirlatmalarAsync(yasliId);
    }

    public async Task<IActionResult> OnPostAcilAsync()
    {
        // Yaşlı kimliği formdan değil oturumdan alınır: başkası adına alarm üretilemez.
        await _olayService.AcilButonAsync(User.YasliId()!.Value);
        Mesaj = "Yakınlarınıza haber verildi.";
        return RedirectToPage();
    }
}
