using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YasliBakim.Business.Abstract;
using YasliBakim.Business.Kurallar;
using YasliBakim.Entities.Concrete;
using YasliBakim.Entities.Enums;
using yaslibakimapp.Guvenlik;

namespace yaslibakimapp.Pages.Aktiviteler;

// Aktiviteyi yaşlı kendisi ya da ona atanmış bakıcı girer. Yönetici yalnızca görüntüler (girdiği kayıt kime ait olurdu belli olmaz).
[Authorize(Roles = Roller.YasliVeyaBakici)]
public class YeniModel : PageModel
{
    private readonly IAktiviteService _aktiviteService;
    private readonly IYasliService _yasliService;
    private readonly IBakiciService _bakiciService;
    private readonly TimeProvider _saat;

    public YeniModel(IAktiviteService aktiviteService, IYasliService yasliService, IBakiciService bakiciService, TimeProvider saat)
    {
        _aktiviteService = aktiviteService;
        _yasliService = yasliService;
        _bakiciService = bakiciService;
        _saat = saat;
    }

    // Formdan yalnızca bu alanlar bağlanır; yaşlı ve kaydeden kişi oturumdan belirlenir.
    public class Girdi
    {
        [Required(ErrorMessage = "Ne yaptığınızı seçin.")]
        public AktiviteKategorisi? Kategori { get; set; }

        [Range(AktiviteKurallari.EnKisaSure, AktiviteKurallari.EnUzunSure, ErrorMessage = "Süre 1 ile 480 dakika arasında olmalı.")]
        public int SureDakika { get; set; } = 30;

        [Required(ErrorMessage = "Zaman zorunlu.")]
        public DateTime Zaman { get; set; }

        [StringLength(AktiviteKurallari.NotUzunlugu, ErrorMessage = "En fazla 500 karakter.")]
        public string? Not { get; set; }
    }

    [BindProperty]
    public Girdi Form { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? YasliId { get; set; }

    public Yasli Yasli { get; private set; } = null!;
    public DateTime Simdi => _saat.GetLocalNow().DateTime;
    public string? Hata { get; set; }

    public static readonly int[] HazirSureler = [10, 15, 30, 45, 60, 90, 120];

    public async Task<IActionResult> OnGetAsync()
    {
        var sonuc = await YukleAsync();
        if (sonuc is not null)
            return sonuc;
        Form.Zaman = Simdi.AddSeconds(-Simdi.Second);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sonuc = await YukleAsync();
        if (sonuc is not null)
            return sonuc;
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _aktiviteService.AktiviteEkleAsync(new AktiviteGirdisi(
                Yasli.Id, User.BakiciId(), Form.Kategori!.Value, Form.SureDakika, Form.Zaman, Form.Not));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            // İş kuralı hatası formda gösterilir, girilen değerler korunur.
            Hata = ex.Message;
            return Page();
        }

        TempData["Mesaj"] = "Aktivite kaydedildi.";
        return User.IsInRole(Roller.Yasli)
            ? RedirectToPage("/Panelim")
            : RedirectToPage("/Aktiviteler/Gecmis", new { yasliId = Yasli.Id });
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
