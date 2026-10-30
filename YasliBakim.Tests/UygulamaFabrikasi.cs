using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using YasliBakim.Business.Guvenlik;
using YasliBakim.DataAccess.Context;
using YasliBakim.DataAccess.Seed;

namespace YasliBakim.Tests;

/// <summary>
/// Uygulamayı bellekte (gerçek HTTP sunucusu açmadan) çalıştırır. SQL Server yerine bellekte SQLite kullanılır,
/// örnek veri ve demo kullanıcılar yüklenir. Böylece giriş, çerez ve rol kontrolleri uçtan uca test edilir.
/// </summary>
public class UygulamaFabrikasi : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _baglanti = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _baglanti.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<YasliBakimDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<YasliBakimDbContext>>();
            services.AddDbContext<YasliBakimDbContext>(o => o.UseSqlite(_baglanti));

            services.RemoveAll<IParolaHasher>();
            services.AddSingleton<IParolaHasher>(new Pbkdf2ParolaHasher(iterasyon: 1_000));

            // Dakikalık arka plan servisi testlerde çalışmaz (aynı bellek veritabanına paralel yazmasın).
            var arkaPlan = services.Single(d => d.ImplementationType == typeof(yaslibakimapp.Servisler.KacirilanHatirlatmaServisi));
            services.Remove(arkaPlan);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YasliBakimDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IParolaHasher>();
        context.Database.EnsureCreated();
        OrnekVeri.YukleAsync(context).GetAwaiter().GetResult();
        OrnekVeri.KullanicilariYukleAsync(context, hasher.Hashle).GetAwaiter().GetResult();

        return host;
    }

    // Canlı ortam ayarında çerez yalnızca HTTPS'te gönderilir; test istemcisi de https kullanır.
    public HttpClient IstemciOlustur() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

    public async Task<HttpResponseMessage> GirisYapAsync(HttpClient istemci, string eposta, string parola)
    {
        var token = await TokenAlAsync(istemci, "/Hesap/Giris");
        return await istemci.PostAsync("/Hesap/Giris", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Eposta"] = eposta,
            ["Parola"] = parola,
            ["__RequestVerificationToken"] = token,
        }));
    }

    // Formdaki anti-forgery (CSRF) token'ını sayfadan okur.
    public static async Task<string> TokenAlAsync(HttpClient istemci, string adres)
    {
        var html = await istemci.GetStringAsync(adres);
        return Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
    }

    public T Sorgula<T>(Func<YasliBakimDbContext, T> sorgu)
    {
        using var scope = Services.CreateScope();
        return sorgu(scope.ServiceProvider.GetRequiredService<YasliBakimDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _baglanti.Dispose();
    }
}
