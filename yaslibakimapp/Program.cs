using Microsoft.EntityFrameworkCore;
using YasliBakim.Business.Abstract;
using YasliBakim.Business.Concrete;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Concrete;
using YasliBakim.DataAccess.Context;
using YasliBakim.DataAccess.Seed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Veri katmanı
builder.Services.AddDbContext<YasliBakimDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
builder.Services.AddScoped<IYasliRepository, EfYasliRepository>();
builder.Services.AddScoped<IBakiciRepository, EfBakiciRepository>();
builder.Services.AddScoped<IHatirlatmaRepository, EfHatirlatmaRepository>();
builder.Services.AddScoped<IAktiviteKaydiRepository, EfAktiviteKaydiRepository>();
builder.Services.AddScoped<IOlayRepository, EfOlayRepository>();

// İş katmanı
builder.Services.AddScoped<IYasliService, YasliManager>();
builder.Services.AddScoped<IBakiciService, BakiciManager>();
builder.Services.AddScoped<IHatirlatmaService, HatirlatmaManager>();
builder.Services.AddScoped<IAktiviteService, AktiviteManager>();
builder.Services.AddScoped<IOlayService, OlayManager>();

var app = builder.Build();

// Geliştirme ortamında bekleyen migration'ları uygula ve boşsa sentetik örnek veriyi yükle.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<YasliBakimDbContext>();
    await context.Database.MigrateAsync();
    await OrnekVeri.YukleAsync(context);
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

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
