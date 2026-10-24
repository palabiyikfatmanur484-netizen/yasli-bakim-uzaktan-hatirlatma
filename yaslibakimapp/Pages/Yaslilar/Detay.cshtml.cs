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

    public DetayModel(IYasliService yasliService, IBakiciService bakiciService, IAktiviteService aktiviteService)
    {
        _yasliService = yasliService;
        _bakiciService = bakiciService;
        _aktiviteService = aktiviteService;
    }

    public Yasli Yasli { get; private set; } = null!;
    public List<AktiviteKaydi> SonKayitlar { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Rol kontrolü yetmez: bakıcı, adres çubuğuna başka bir id yazarak (IDOR)
        // kendisine atanmamış yaşlının sağlık bilgilerini görememeli.
        var bakiciId = User.BakiciId();
        if (bakiciId is not null && !await _bakiciService.YasliyaAtanmisMiAsync(bakiciId.Value, id))
            return Forbid();

        var yasli = await _yasliService.DetayGetirAsync(id);
        if (yasli is null)
            return NotFound();

        Yasli = yasli;
        SonKayitlar = await _aktiviteService.SonKayitlarAsync(id, gun: 7);
        return Page();
    }
}
