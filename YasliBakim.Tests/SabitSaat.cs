namespace YasliBakim.Tests;

/// <summary>
/// Testlerde "şu an"ı istediğimiz zamana sabitler (ör. bugün 09:10). İleri alınabilir.
/// Saat dilimi UTC kabul edilir; böylece yerel saat = verilen saat olur.
/// </summary>
public class SabitSaat : TimeProvider
{
    private DateTime _simdi;

    public SabitSaat(DateTime simdi)
    {
        _simdi = simdi;
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(_simdi, DateTimeKind.Utc));

    public void Ayarla(DateTime simdi) => _simdi = simdi;

    public void IleriAl(TimeSpan sure) => _simdi += sure;
}
