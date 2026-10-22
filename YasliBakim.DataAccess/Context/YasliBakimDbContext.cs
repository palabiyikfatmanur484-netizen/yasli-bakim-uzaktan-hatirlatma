using Microsoft.EntityFrameworkCore;
using YasliBakim.Entities.Concrete;

namespace YasliBakim.DataAccess.Context;

public class YasliBakimDbContext : DbContext
{
    public YasliBakimDbContext(DbContextOptions<YasliBakimDbContext> options) : base(options)
    {
    }

    public DbSet<Yasli> Yaslilar => Set<Yasli>();
    public DbSet<Bakici> Bakicilar => Set<Bakici>();
    public DbSet<YasliBakici> YasliBakicilar => Set<YasliBakici>();
    public DbSet<Hatirlatma> Hatirlatmalar => Set<Hatirlatma>();
    public DbSet<AktiviteKaydi> AktiviteKayitlari => Set<AktiviteKaydi>();
    public DbSet<Olay> Olaylar => Set<Olay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Yasli>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(50).IsRequired();
            e.Property(x => x.Soyad).HasMaxLength(50).IsRequired();
            e.Property(x => x.Telefon).HasMaxLength(20);
            e.Property(x => x.Adres).HasMaxLength(250);
        });

        modelBuilder.Entity<Bakici>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(50).IsRequired();
            e.Property(x => x.Soyad).HasMaxLength(50).IsRequired();
            e.Property(x => x.Eposta).HasMaxLength(100).IsRequired();
            e.Property(x => x.Telefon).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Eposta).IsUnique();
        });

        // N-N: Yaşlı <-> Bakıcı, ara tablo YasliBakici (bileşik anahtar)
        modelBuilder.Entity<YasliBakici>(e =>
        {
            e.HasKey(x => new { x.YasliId, x.BakiciId });
            e.HasOne(x => x.Yasli).WithMany(y => y.Bakicilar).HasForeignKey(x => x.YasliId);
            e.HasOne(x => x.Bakici).WithMany(b => b.Yaslilar).HasForeignKey(x => x.BakiciId);
            e.Property(x => x.YakinlikDerecesi).HasMaxLength(30);
        });

        // 1-N: Yaşlı -> Hatırlatma, Bakıcı -> Hatırlatma (oluşturan)
        modelBuilder.Entity<Hatirlatma>(e =>
        {
            e.Property(x => x.Baslik).HasMaxLength(100).IsRequired();
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasOne(x => x.Yasli).WithMany(y => y.Hatirlatmalar)
                .HasForeignKey(x => x.YasliId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.OlusturanBakici).WithMany(b => b.OlusturduguHatirlatmalar)
                .HasForeignKey(x => x.OlusturanBakiciId).OnDelete(DeleteBehavior.SetNull);
        });

        // 1-N: Yaşlı -> AktiviteKaydi, Hatırlatma -> AktiviteKaydi
        modelBuilder.Entity<AktiviteKaydi>(e =>
        {
            e.Property(x => x.Not).HasMaxLength(500);
            e.HasOne(x => x.Yasli).WithMany(y => y.AktiviteKayitlari)
                .HasForeignKey(x => x.YasliId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Hatirlatma).WithMany(h => h.AktiviteKayitlari)
                .HasForeignKey(x => x.HatirlatmaId).OnDelete(DeleteBehavior.NoAction);
        });

        // 1-N: Yaşlı -> Olay, Bakıcı -> Olay (ilgilenen); 1-1 (0..1): AktiviteKaydi -> Olay
        modelBuilder.Entity<Olay>(e =>
        {
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasOne(x => x.Yasli).WithMany(y => y.Olaylar)
                .HasForeignKey(x => x.YasliId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.IlgilenenBakici).WithMany(b => b.IlgilendigiOlaylar)
                .HasForeignKey(x => x.IlgilenenBakiciId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AktiviteKaydi).WithOne()
                .HasForeignKey<Olay>(x => x.AktiviteKaydiId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
