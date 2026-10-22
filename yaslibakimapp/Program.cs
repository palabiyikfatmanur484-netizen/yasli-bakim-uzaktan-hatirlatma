using Microsoft.EntityFrameworkCore;
using YasliBakim.Business.Abstract;
using YasliBakim.Business.Concrete;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Concrete;
using YasliBakim.DataAccess.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Veri katmanı
builder.Services.AddDbContext<YasliBakimDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));

// İş katmanı
builder.Services.AddScoped<IYasliService, YasliManager>();

var app = builder.Build();

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
