using YasliBakim.Business.Abstract;

namespace yaslibakimapp.Servisler;

/// <summary>
/// Uygulama açık olduğu sürece her dakika çalışır ve süresi geçtiği hâlde yanıtlanmamış
/// hatırlatmaları "Kaçırıldı" olarak işaretler (ilaç/randevu ise yakınlara olay oluşur).
/// İş mantığı HatirlatmaTakipManager'dadır; bu sınıf yalnızca zamanlayıcıdır.
/// </summary>
public class KacirilanHatirlatmaServisi : BackgroundService
{
    private static readonly TimeSpan Aralik = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KacirilanHatirlatmaServisi> _logger;

    public KacirilanHatirlatmaServisi(IServiceScopeFactory scopeFactory, ILogger<KacirilanHatirlatmaServisi> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var zamanlayici = new PeriodicTimer(Aralik);
        do
        {
            try
            {
                // DbContext "scoped" olduğu için her turda yeni bir scope açılır.
                using var scope = _scopeFactory.CreateScope();
                var takip = scope.ServiceProvider.GetRequiredService<IHatirlatmaTakipService>();
                var sayi = await takip.KacirilanlariIsleAsync();
                if (sayi > 0)
                    _logger.LogInformation("{Sayi} hatırlatma kaçırıldı olarak işaretlendi.", sayi);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Bir turdaki hata servisi durdurmasın; bir sonraki dakikada tekrar denenir.
                _logger.LogError(ex, "Kaçırılan hatırlatma kontrolü başarısız.");
            }
        }
        while (await zamanlayici.WaitForNextTickAsync(stoppingToken));
    }
}
