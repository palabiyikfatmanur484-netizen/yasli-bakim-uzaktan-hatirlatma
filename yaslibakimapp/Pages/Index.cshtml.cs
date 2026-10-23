using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Entities.Concrete;

namespace yaslibakimapp.Pages;

public class IndexModel : PageModel
{
    private readonly IYasliService _yasliService;
    private readonly IHatirlatmaService _hatirlatmaService;
    private readonly IOlayService _olayService;

    public IndexModel(IYasliService yasliService, IHatirlatmaService hatirlatmaService, IOlayService olayService)
    {
        _yasliService = yasliService;
        _hatirlatmaService = hatirlatmaService;
        _olayService = olayService;
    }

    public List<(Yasli Yasli, List<Hatirlatma> Bugun)> Yaslilar { get; } = new();
    public List<Olay> AcikOlaylar { get; private set; } = new();

    public async Task OnGetAsync()
    {
        foreach (var y in await _yasliService.TumunuGetirAsync())
        {
            var detay = await _yasliService.DetayGetirAsync(y.Id);
            Yaslilar.Add((detay!, await _hatirlatmaService.BugunkuHatirlatmalarAsync(y.Id)));
        }

        AcikOlaylar = await _olayService.AcikOlaylarAsync();
    }
}
