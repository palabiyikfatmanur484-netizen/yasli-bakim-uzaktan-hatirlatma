# Veritabanı Tasarımı ve Veri Erişim Katmanı (Hafta 4)

Şema, [ER diyagramındaki](ER-Diyagrami.md) altı varlıktan **EF Core Code-First** ile üretilir. Kısıtlar ve indeksler `YasliBakimDbContext.OnModelCreating` içinde Fluent API ile tanımlıdır; migration bu tanımdan SQL üretir.

| Dosya | İçerik |
|---|---|
| [YasliBakimDbContext.cs](../YasliBakim.DataAccess/Context/YasliBakimDbContext.cs) | Tablolar, ilişkiler, kısıtlar, indeksler |
| [Migrations/](../YasliBakim.DataAccess/Migrations/) | `IlkSema` migration'ı |
| [database/01_sema.sql](../database/01_sema.sql) | Migration'dan üretilen SQL script (DDL) |
| [database/02_ornek_sorgular.sql](../database/02_ornek_sorgular.sql) | JOIN/GROUP BY sorguları, transaction içinde CRUD, kısıt denemeleri |
| [OrnekVeri.cs](../YasliBakim.DataAccess/Seed/OrnekVeri.cs) | Sentetik örnek veri |
| [YasliBakim.Tests/](../YasliBakim.Tests/) | CRUD, ilişki ve kısıt testleri (13 test) |

## Tablolar

| Tablo | Birincil anahtar | Satır (örnek veri) |
|---|---|---|
| Yaslilar | `Id` (IDENTITY) | 5 |
| Bakicilar | `Id` | 4 |
| YasliBakicilar | `(YasliId, BakiciId)` bileşik | 8 |
| Hatirlatmalar | `Id` | 12 |
| AktiviteKayitlari | `Id` | ~40 |
| Olaylar | `Id` | 3 |

## Kısıtlar

### Yabancı anahtarlar ve silme davranışı

| FK | Silme kuralı | Neden |
|---|---|---|
| YasliBakicilar → Yaslilar, Bakicilar | `CASCADE` | Yaşlı veya bakıcı silinince eşleştirmenin anlamı kalmaz. |
| Hatirlatmalar → Yaslilar | `CASCADE` | Yaşlının hatırlatmaları ona aittir. |
| Hatirlatmalar → Bakicilar (oluşturan) | `SET NULL` | Bakıcı ayrılsa da hatırlatma çalışmaya devam etmeli. |
| AktiviteKayitlari → Yaslilar | `CASCADE` | |
| AktiviteKayitlari → Hatirlatmalar | `NO ACTION` | Geçmiş onay kayıtları kaybolmasın: onay geçmişi olan hatırlatma silinemez, pasife alınır. Ayrıca SQL Server'da Yaslilar'dan iki ayrı cascade yolu oluşmasını engeller. |
| Olaylar → Yaslilar | `CASCADE` | |
| Olaylar → Bakicilar (ilgilenen) | `SET NULL` | |
| Olaylar → AktiviteKayitlari | `NO ACTION` + filtreli **UNIQUE** indeks | 1-1 (0..1): bir aktivite en fazla bir olay üretir. |

### CHECK ve UNIQUE kısıtları

| Kısıt | Kural |
|---|---|
| `CK_Yasli_DogumTarihi` | `DogumTarihi >= '1900-01-01'` |
| `CK_Hatirlatma_Tarih` | `BitisTarihi IS NULL OR BitisTarihi >= BaslangicTarihi` |
| `CK_AktiviteKaydi_Tur` | Hatırlatma onayında (`Tur=1`) hatırlatma ve durum dolu; günlük durumda (`Tur=2`) hatırlatma boş, ruh hali dolu. |
| `CK_Olay_CozulmeZamani` | `CozulmeZamani IS NULL OR CozulmeZamani >= OlusmaZamani` |
| `UX_Bakici_Eposta` | Aynı e-posta ile iki bakıcı olamaz. |
| `NOT NULL` + uzunluk | Ad/Soyad 50, Eposta 100, Baslik 100, açıklama/not alanları 500 karakter. |

İş katmanı aynı kuralları önceden kontrol edip anlaşılır hata mesajı verir; veritabanı kısıtları ise iş katmanı atlansa bile (ör. elle SQL) verinin bozulmasını engelleyen son savunma hattıdır.

### Elle yazılmış ilişki ve kısıt (açıklamalı)

EF Core'un ürettiği DDL'in N-N ara tablo ve `CK_AktiviteKaydi_Tur` kısmının elle yazılmış hali:

```sql
CREATE TABLE YasliBakicilar (
    YasliId           int          NOT NULL,
    BakiciId          int          NOT NULL,
    YakinlikDerecesi  nvarchar(30) NULL,
    BirincilSorumluMu bit          NOT NULL,
    AtanmaTarihi      datetime2    NOT NULL,
    -- Bileşik PK: aynı yaşlı-bakıcı çifti ikinci kez eklenemez.
    CONSTRAINT PK_YasliBakicilar PRIMARY KEY (YasliId, BakiciId),
    -- İki FK: her satır var olan bir yaşlıya ve var olan bir bakıcıya işaret etmek zorunda.
    CONSTRAINT FK_YasliBakicilar_Yaslilar  FOREIGN KEY (YasliId)  REFERENCES Yaslilar(Id)  ON DELETE CASCADE,
    CONSTRAINT FK_YasliBakicilar_Bakicilar FOREIGN KEY (BakiciId) REFERENCES Bakicilar(Id) ON DELETE CASCADE
);
-- PK (YasliId, BakiciId) "yaşlının bakıcıları" sorgusunu karşılar; ters yön için ayrı indeks:
CREATE INDEX IX_YasliBakici_BakiciId ON YasliBakicilar (BakiciId);

-- Aktivite kaydının türüne göre hangi alanların dolu olacağını veritabanı garanti eder.
ALTER TABLE AktiviteKayitlari ADD CONSTRAINT CK_AktiviteKaydi_Tur CHECK (
       (Tur = 1 AND HatirlatmaId IS NOT NULL AND Durum IS NOT NULL)    -- hatırlatma onayı
    OR (Tur = 2 AND HatirlatmaId IS NULL     AND RuhHali IS NOT NULL)  -- günlük durum
);
```

## İndeksler

FK sütunlarına EF Core otomatik indeks ekler. Bunlara ek olarak, uygulamanın **en sık yapacağı sorgulara** göre bileşik indeksler eklendi:

| İndeks | Sütunlar | Hangi sorgu için |
|---|---|---|
| `IX_Hatirlatma_Yasli_Aktif_Saat` | YasliId, AktifMi, Saat | "Bu yaşlının bugünkü aktif hatırlatmaları, saate göre" — hatırlatma ekranı her açılışta çalıştırır. YasliId ile başladığı için ayrı FK indeksine gerek kalmaz. |
| `IX_AktiviteKaydi_Yasli_Zaman` | YasliId, KayitZamani | Yaşlının son N günlük zaman çizelgesi; tablo en hızlı büyüyen tablodur. |
| `IX_Olay_Durum_Seviye` | Durum, Seviye | Yakın paneli: açık olaylar, en kritik önce. |
| `IX_Olay_Yasli_Zaman` | YasliId, OlusmaZamani | Bir yaşlının olay geçmişi. |
| `IX_Yasli_Soyad_Ad` | Soyad, Ad | Ada göre sıralı liste ve arama. |
| `UX_Bakici_Eposta` | Eposta (UNIQUE) | Giriş e-postası ile arama + tekrar kaydı engelleme. |
| `IX_YasliBakici_BakiciId` | BakiciId | "Bu bakıcının yaşlıları" (bileşik PK'nın ikinci sütunu tek başına aranamaz). |

**Eklenmeyen indeksler:** `Telefon`, `Adres`, `Aciklama`, `Not` gibi alanlarda arama yapılmıyor; `Tur` gibi az farklı değer alan (düşük seçicilikli) sütunlar tek başına indekslenmedi. Her indeks INSERT/UPDATE işlemlerini yavaşlatır ve yer kaplar; bu yüzden yalnızca gerçekten sorgulanan sütunlara eklendi.

## Migration

```bash
dotnet tool restore                                   # dotnet-ef aracını kurar (dotnet-tools.json)
dotnet ef migrations add <Ad> --project YasliBakim.DataAccess --startup-project yaslibakimapp
dotnet ef database update     --project YasliBakim.DataAccess --startup-project yaslibakimapp
dotnet ef migrations script --idempotent --project YasliBakim.DataAccess --startup-project yaslibakimapp -o database/01_sema.sql
```

Uygulama Development ortamında açılırken bekleyen migration'ları kendisi uygular ve veritabanı boşsa örnek veriyi yükler (`Program.cs`).

## ORM mi saf SQL mi?

| | EF Core (seçilen) | Saf SQL (ADO.NET / Dapper) |
|---|---|---|
| Şema | C# sınıflarından migration ile üretilir, sürüm kontrolünde tutulur | `.sql` dosyaları elle yazılır ve sırayla çalıştırılır |
| Sorgu | LINQ, derleme zamanında tip kontrolü | SQL metni, hatalar çalışma zamanında çıkar |
| SQL injection | Parametreler otomatik | Parametre kullanımı geliştiriciye kalmış |
| Performans | Basit CRUD için yeterli; karmaşık raporlarda üretilen SQL kontrol edilmeli | Tam kontrol |

Projede CRUD ağırlıklı ve şema sık değişecek; bu yüzden EF Core seçildi. Rapor tipi sorgular (ör. ilaç uyum oranı) [02_ornek_sorgular.sql](../database/02_ornek_sorgular.sql) dosyasında saf SQL olarak da gösterildi.

## Örnek veri ve KVKK

Tüm kayıtlar **sentetiktir**: isimler uydurmadır (Örnekoğlu, Testçi, Sentetik…), telefonlar `0500 000 xx xx` aralığında, e-postalar gerçek olmayan `ornek.test` alan adındadır, adresler "Örnek Mah." gibi hayali adreslerdir. Bu kural `OrnekVeri_YuklenirVeSentetiktir` testi ile kontrol edilir. Gerçek kişisel veya sağlık verisi kullanılmamıştır.

Senaryolar: Hasan Bey'in dün akşam ilacını atlaması (→ *KacirilanHatirlatma* olayı), Mehmet Bey'in kötü ruh hali bildirmesi (→ *Anomali*), Ayşe Hanım'ın 5 gün önce acil butona basması (→ çözülmüş *AcilButon* olayı).

## CRUD veri erişimi

- `IGenericRepository<T>` / `EfGenericRepository<T>`: tüm tablolar için ortak Create/Read/Update/Delete. Bileşik anahtar için `GetByKeyAsync(yasliId, bakiciId)`.
- Tabloya özel repository'ler (`IYasliRepository`, `IHatirlatmaRepository`, `IOlayRepository` …): JOIN (`Include`) ve filtre gerektiren sorgular.
- Servisler (`YasliManager`, `BakiciManager`, `HatirlatmaManager`, `AktiviteManager`, `OlayManager`): doğrulama + iş kuralları.

**Transaction:** Tüm repository'ler aynı istek içinde aynı `DbContext`'i paylaşır. `AktiviteManager.HatirlatmaOnaylaAsync` atlanan ilaçta hem aktivite kaydını hem olayı ekler ve tek `SaveChanges` çağırır; EF Core bunu tek transaction içinde yazar, biri başarısız olursa ikisi de yazılmaz.

## Sözlü sınav soruları

**1. Bir tabloya neden indeks ekledin / eklemedin?**
İndeksi, uygulamanın sık çalıştırdığı sorgunun `WHERE` ve `ORDER BY` sütunlarına göre ekledim. Örneğin hatırlatma ekranı her açıldığında `WHERE YasliId = @id AND AktifMi = 1 ORDER BY Saat` çalışır; `(YasliId, AktifMi, Saat)` indeksi sayesinde SQL Server tüm tabloyu taramak yerine doğrudan o yaşlının satırlarına gider ve sıralama yapmaz. Telefon, adres, not gibi alanlarda arama yapılmadığı için indeks eklemedim; her indeks yazma işlemlerini yavaşlatır ve disk kaplar.

**2. Yabancı anahtar kısıtı ne işe yarar?**
Referans bütünlüğünü korur: bir satırın işaret ettiği kaydın gerçekten var olmasını garanti eder. Örneğin `Hatirlatmalar.YasliId = 99999` olan bir kayıt eklenemez, çünkü öyle bir yaşlı yok (`02_ornek_sorgular.sql` 6. bölüm). Silme davranışını da belirler: yaşlı silinince hatırlatmaları `CASCADE` ile silinir, bakıcı silinince hatırlatmadaki `OlusturanBakiciId` `SET NULL` olur.

**3. N-N ilişkiyi nasıl modelledin?**
Bir yaşlıya birden fazla bakıcı, bir bakıcıya birden fazla yaşlı düşebilir. Bunu `YasliBakicilar` ara tablosu ile iki adet 1-N ilişkiye böldüm. Ara tablonun PK'sı `(YasliId, BakiciId)` bileşik anahtarıdır; aynı eşleştirme iki kez eklenemez. İlişkiye ait bilgiler (`YakinlikDerecesi`, `BirincilSorumluMu`, `AtanmaTarihi`) de bu tabloda tutulur, çünkü bunlar ne yalnız yaşlıya ne yalnız bakıcıya aittir.

**4. Migration neden kullanılır?**
Veritabanı şemasındaki değişiklikleri kod gibi sürümlemek için. Her migration, şemayı bir önceki halden yenisine taşıyan `Up` ve geri alan `Down` adımlarını içerir ve git'te saklanır. Böylece projeyi klonlayan herkes `dotnet ef database update` ile aynı şemayı elde eder, hangi değişikliğin ne zaman yapıldığı izlenir, gerekirse geri alınır. Elle tablo değiştirmekten kaynaklanan "benim bilgisayarımda çalışıyordu" sorunları ortadan kalkar.

**5. Bir CRUD işleminin SQL'ini açıkla.**
`BakiciManager.YasliyaAtaAsync` → `INSERT INTO YasliBakicilar (YasliId, BakiciId, YakinlikDerecesi, BirincilSorumluMu, AtanmaTarihi) VALUES (@p0, @p1, @p2, @p3, @p4)`. EF Core değerleri parametre olarak gönderir (SQL injection'a karşı güvenli). Veritabanı önce iki FK'yı kontrol eder (yaşlı ve bakıcı var mı), sonra PK'yı (bu çift zaten var mı). Güncelleme: `UPDATE Yaslilar SET Telefon = @p0 WHERE Id = @p1`; silme: `DELETE FROM Yaslilar WHERE Id = @p0` — bu satır silinince FK'lardaki `ON DELETE CASCADE` sayesinde yaşlının hatırlatma, aktivite ve olay kayıtları da silinir. Tamamı transaction içinde `02_ornek_sorgular.sql` 5. bölümde çalıştırılabilir.

## Teorik kavramlar

- **DDL** (Data Definition Language): yapıyı tanımlar — `CREATE TABLE`, `ALTER TABLE`, `CREATE INDEX`. → `01_sema.sql`
- **DML** (Data Manipulation Language): veriyi işler — `SELECT`, `INSERT`, `UPDATE`, `DELETE`. → `02_ornek_sorgular.sql`
- **İndeks:** Tablonun belirli sütunlara göre sıralı bir kopyası (B-tree); aramayı tam taramadan (O(n)) ağaç aramasına (O(log n)) indirir.
- **Referans bütünlüğü:** FK'ların her zaman var olan bir satırı göstermesi.
- **Transaction:** Birden fazla işlemin "ya hepsi ya hiçbiri" çalışması (ACID). `BEGIN TRANSACTION … COMMIT/ROLLBACK`.

## Yapay zekâ kullanımı

Yapay zekâ; indeks önerileri (hangi sorgu hangi bileşik indeksi kullanır), check constraint ifadeleri, SQL Server'daki çoklu cascade yolu sorunu ve migration komutları için kullanıldı. Öneriler, testler (`dotnet test`) ve `02_ornek_sorgular.sql` LocalDB üzerinde çalıştırılarak doğrulandı.
