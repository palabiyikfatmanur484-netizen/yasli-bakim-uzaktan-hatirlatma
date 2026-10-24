using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Entities.Concrete;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages;

// Yakın paneli: yönetici her yaşlıyı, bakıcı yalnızca kendisine atanmış yaşlıları görür.
[Authorize(Roles = Roller.YoneticiVeyaBakici)]
public class IndexModel : PageModel
{
    private readonly IYasliService _yasliService;
    private readonly IBakiciService _bakiciService;
    private readonly IHatirlatmaService _hatirlatmaService;
    private readonly IOlayService _olayService;

    public IndexModel(IYasliService yasliService, IBakiciService bakiciService,
        IHatirlatmaService hatirlatmaService, IOlayService olayService)
    {
        _yasliService = yasliService;
        _bakiciService = bakiciService;
        _hatirlatmaService = hatirlatmaService;
        _olayService = olayService;
    }

    public List<(Yasli Yasli, List<Hatirlatma> Bugun)> Yaslilar { get; } = new();
    public List<Olay> AcikOlaylar { get; private set; } = new();

    public async Task OnGetAsync()
    {
        // Filtre sunucuda uygulanır; bakıcının göremeyeceği kayıtlar sayfaya hiç gelmez.
        var bakiciId = User.BakiciId();
        var yaslilar = bakiciId is null
            ? await _yasliService.TumunuGetirAsync()
            : await _bakiciService.YaslilariGetirAsync(bakiciId.Value);

        foreach (var y in yaslilar)
        {
            var detay = await _yasliService.DetayGetirAsync(y.Id);
            Yaslilar.Add((detay!, await _hatirlatmaService.BugunkuHatirlatmalarAsync(y.Id)));
        }

        AcikOlaylar = await _olayService.AcikOlaylarAsync(bakiciId);
    }
}
