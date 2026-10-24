using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Entities.Concrete;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages.Yonetim;

[Authorize(Roles = Roller.Yonetici)]
public class KullanicilarModel : PageModel
{
    private readonly IKimlikService _kimlikService;

    public KullanicilarModel(IKimlikService kimlikService)
    {
        _kimlikService = kimlikService;
    }

    public List<Kullanici> Kullanicilar { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Kullanicilar = await _kimlikService.TumKullanicilarAsync();
    }

    public async Task<IActionResult> OnPostKilidiKaldirAsync(int id)
    {
        await _kimlikService.KilidiKaldirAsync(id);
        return RedirectToPage();
    }
}
