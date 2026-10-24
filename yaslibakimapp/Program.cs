using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using YasliBakim.Business.Abstract;
using YasliBakim.Business.Concrete;
using YasliBakim.Business.Guvenlik;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Concrete;
using YasliBakim.DataAccess.Context;
using YasliBakim.DataAccess.Seed;
using yaslibakimapp.Guvenlik;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Kimlik doğrulama: şifreli + imzalı oturum çerezi
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Hesap/Giris";
        options.LogoutPath = "/Hesap/Cikis";
        options.AccessDeniedPath = "/Hesap/ErisimEngellendi";

        // Oturum 30 dk işlem yapılmazsa biter; her istekte süre yenilenir.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.Cookie.Name = "YasliBakim.Oturum";
        options.Cookie.HttpOnly = true;                       // JavaScript çereze erişemez (XSS ile çalınamaz)
        options.Cookie.SameSite = SameSiteMode.Lax;           // Başka siteden gelen POST'larda gönderilmez (CSRF)
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest                // geliştirmede http://localhost için
            : CookieSecurePolicy.Always;                      // canlıda yalnızca HTTPS

        options.Events.OnValidatePrincipal = OturumDogrulayici.DogrulaAsync;
    });

// Yetkilendirme: varsayılan olarak her sayfa giriş ister; herkese açık sayfalar [AllowAnonymous] ile işaretlenir.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Veri katmanı
builder.Services.AddDbContext<YasliBakimDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
builder.Services.AddScoped<IYasliRepository, EfYasliRepository>();
builder.Services.AddScoped<IBakiciRepository, EfBakiciRepository>();
builder.Services.AddScoped<IHatirlatmaRepository, EfHatirlatmaRepository>();
builder.Services.AddScoped<IAktiviteKaydiRepository, EfAktiviteKaydiRepository>();
builder.Services.AddScoped<IOlayRepository, EfOlayRepository>();
builder.Services.AddScoped<IKullaniciRepository, EfKullaniciRepository>();

// İş katmanı
builder.Services.AddSingleton<IParolaHasher, Pbkdf2ParolaHasher>();
builder.Services.AddScoped<IYasliService, YasliManager>();
builder.Services.AddScoped<IBakiciService, BakiciManager>();
builder.Services.AddScoped<IHatirlatmaService, HatirlatmaManager>();
builder.Services.AddScoped<IAktiviteService, AktiviteManager>();
builder.Services.AddScoped<IOlayService, OlayManager>();
builder.Services.AddScoped<IKimlikService, KimlikManager>();

var app = builder.Build();

// Geliştirme ortamında bekleyen migration'ları uygula ve boşsa sentetik örnek veriyi yükle.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<YasliBakimDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IParolaHasher>();
    await context.Database.MigrateAsync();
    await OrnekVeri.YukleAsync(context);
    await OrnekVeri.KullanicilariYukleAsync(context, hasher.Hashle);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Sıra önemli: önce kim olduğunu bul (authentication), sonra neye yetkisi var bak (authorization).
app.UseAuthentication();
app.UseAuthorization();

// CSS/JS dosyaları giriş sayfasında da gerekli; varsayılan "giriş zorunlu" kuralından muaf.
app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Entegrasyon testlerinde WebApplicationFactory<Program> için.
public partial class Program;
