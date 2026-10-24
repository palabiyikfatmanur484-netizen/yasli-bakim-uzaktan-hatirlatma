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
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Yasli>(e =>
        {
            e.ToTable("Yaslilar", t =>
                t.HasCheckConstraint("CK_Yasli_DogumTarihi", "[DogumTarihi] >= '1900-01-01'"));

            e.Property(x => x.Ad).HasMaxLength(50).IsRequired();
            e.Property(x => x.Soyad).HasMaxLength(50).IsRequired();
            e.Property(x => x.Telefon).HasMaxLength(20);
            e.Property(x => x.Adres).HasMaxLength(250);
            e.Property(x => x.KronikHastaliklar).HasMaxLength(500);
            e.Property(x => x.AcilDurumNotu).HasMaxLength(500);

            // Yaşlı listesi soyada göre sıralanıp aranır.
            e.HasIndex(x => new { x.Soyad, x.Ad }).HasDatabaseName("IX_Yasli_Soyad_Ad");
        });

        modelBuilder.Entity<Bakici>(e =>
        {
            e.ToTable("Bakicilar");

            e.Property(x => x.Ad).HasMaxLength(50).IsRequired();
            e.Property(x => x.Soyad).HasMaxLength(50).IsRequired();
            e.Property(x => x.Eposta).HasMaxLength(100).IsRequired();
            e.Property(x => x.Telefon).HasMaxLength(20).IsRequired();

            // Giriş e-posta ile yapılacak: hem arama hızı hem tekrar kaydın engellenmesi için.
            e.HasIndex(x => x.Eposta).IsUnique().HasDatabaseName("UX_Bakici_Eposta");
        });

        // N-N: Yaşlı <-> Bakıcı, ara tablo YasliBakici (bileşik anahtar)
        modelBuilder.Entity<YasliBakici>(e =>
        {
            e.ToTable("YasliBakicilar");

            e.HasKey(x => new { x.YasliId, x.BakiciId });
            e.HasOne(x => x.Yasli).WithMany(y => y.Bakicilar)
                .HasForeignKey(x => x.YasliId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Bakici).WithMany(b => b.Yaslilar)
                .HasForeignKey(x => x.BakiciId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.YakinlikDerecesi).HasMaxLength(30);

            // Bileşik PK (YasliId, BakiciId) "bu yaşlının bakıcıları" sorgusunu karşılar;
            // "bu bakıcının yaşlıları" sorgusu için BakiciId üzerinde ayrı indeks gerekir.
            e.HasIndex(x => x.BakiciId).HasDatabaseName("IX_YasliBakici_BakiciId");
        });

        // 1-N: Yaşlı -> Hatırlatma, Bakıcı -> Hatırlatma (oluşturan)
        modelBuilder.Entity<Hatirlatma>(e =>
        {
            e.ToTable("Hatirlatmalar", t =>
                t.HasCheckConstraint("CK_Hatirlatma_Tarih",
                    "[BitisTarihi] IS NULL OR [BitisTarihi] >= [BaslangicTarihi]"));

            e.Property(x => x.Baslik).HasMaxLength(100).IsRequired();
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasOne(x => x.Yasli).WithMany(y => y.Hatirlatmalar)
                .HasForeignKey(x => x.YasliId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.OlusturanBakici).WithMany(b => b.OlusturduguHatirlatmalar)
                .HasForeignKey(x => x.OlusturanBakiciId).OnDelete(DeleteBehavior.SetNull);

            // En sık sorgu: "bu yaşlının aktif hatırlatmaları, saate göre".
            // YasliId ile başladığı için FK indeksinin yerini de tutar.
            e.HasIndex(x => new { x.YasliId, x.AktifMi, x.Saat }).HasDatabaseName("IX_Hatirlatma_Yasli_Aktif_Saat");
        });

        // 1-N: Yaşlı -> AktiviteKaydi, Hatırlatma -> AktiviteKaydi
        modelBuilder.Entity<AktiviteKaydi>(e =>
        {
            // Hatırlatma onayında hatırlatma ve durum zorunlu; günlük durumda hatırlatma boş, ruh hali zorunlu.
            e.ToTable("AktiviteKayitlari", t =>
                t.HasCheckConstraint("CK_AktiviteKaydi_Tur",
                    "([Tur] = 1 AND [HatirlatmaId] IS NOT NULL AND [Durum] IS NOT NULL) OR " +
                    "([Tur] = 2 AND [HatirlatmaId] IS NULL AND [RuhHali] IS NOT NULL)"));

            e.Property(x => x.Not).HasMaxLength(500);
            e.HasOne(x => x.Yasli).WithMany(y => y.AktiviteKayitlari)
                .HasForeignKey(x => x.YasliId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Hatirlatma).WithMany(h => h.AktiviteKayitlari)
                .HasForeignKey(x => x.HatirlatmaId).OnDelete(DeleteBehavior.NoAction);

            // Yaşlının zaman çizelgesi: tarihe göre son kayıtlar.
            e.HasIndex(x => new { x.YasliId, x.KayitZamani }).HasDatabaseName("IX_AktiviteKaydi_Yasli_Zaman");
        });

        // 1-N: Yaşlı -> Olay, Bakıcı -> Olay (ilgilenen); 1-1 (0..1): AktiviteKaydi -> Olay
        modelBuilder.Entity<Olay>(e =>
        {
            e.ToTable("Olaylar", t =>
                t.HasCheckConstraint("CK_Olay_CozulmeZamani",
                    "[CozulmeZamani] IS NULL OR [CozulmeZamani] >= [OlusmaZamani]"));

            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasOne(x => x.Yasli).WithMany(y => y.Olaylar)
                .HasForeignKey(x => x.YasliId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.IlgilenenBakici).WithMany(b => b.IlgilendigiOlaylar)
                .HasForeignKey(x => x.IlgilenenBakiciId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AktiviteKaydi).WithOne()
                .HasForeignKey<Olay>(x => x.AktiviteKaydiId).OnDelete(DeleteBehavior.NoAction);

            // Yakın paneli: açık olaylar, önce en kritik olanlar.
            e.HasIndex(x => new { x.Durum, x.Seviye }).HasDatabaseName("IX_Olay_Durum_Seviye");
            e.HasIndex(x => new { x.YasliId, x.OlusmaZamani }).HasDatabaseName("IX_Olay_Yasli_Zaman");
        });

        // 1-1 (0..1): Bakici -> Kullanici, Yasli -> Kullanici
        modelBuilder.Entity<Kullanici>(e =>
        {
            // Rol ile bağlı kayıt tutarlı olmalı: bakıcı hesabı yalnız bakıcıya, yaşlı hesabı yalnız yaşlıya bağlanır.
            e.ToTable("Kullanicilar", t =>
                t.HasCheckConstraint("CK_Kullanici_Rol",
                    "([Rol] = 1 AND [BakiciId] IS NULL AND [YasliId] IS NULL) OR " +
                    "([Rol] = 2 AND [BakiciId] IS NOT NULL AND [YasliId] IS NULL) OR " +
                    "([Rol] = 3 AND [YasliId] IS NOT NULL AND [BakiciId] IS NULL)"));

            e.Property(x => x.Eposta).HasMaxLength(100).IsRequired();
            e.Property(x => x.ParolaHash).HasMaxLength(200).IsRequired();

            // Giriş sorgusu e-posta ile yapılır; aynı e-posta ile iki hesap açılamaz.
            e.HasIndex(x => x.Eposta).IsUnique().HasDatabaseName("UX_Kullanici_Eposta");

            e.HasOne(x => x.Bakici).WithOne()
                .HasForeignKey<Kullanici>(x => x.BakiciId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Yasli).WithOne()
                .HasForeignKey<Kullanici>(x => x.YasliId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
