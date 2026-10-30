# Yaşlı Bireyler İçin Uzaktan Bakım ve Hatırlatma

Yalnız yaşayan yaşlı bireylerin ilaç, randevu ve günlük rutinlerini hatırlatan; günlük durumlarını kaydeden ve olağan dışı bir durumda (kaçırılan ilaç, acil buton vb.) yakınlarını bilgilendiren web uygulaması.

> **Durum:** Hafta 6: İlaç/Aktivite Hatırlatma modülü (tekrarlama, zamanlama, Yaptım/Atla onayı, kaçırılan doz alarmı) tamamlandı.

## Modüller

| Modül | Açıklama |
|---|---|
| Yaşlı profili | Kişisel bilgiler, kronik hastalıklar, acil durum notu |
| Hatırlatma | İlaç / randevu / su içme vb. zamanlanmış hatırlatmalar |
| Günlük durum | Yaşlının hatırlatma onayları ve ruh hali bildirimi |
| Anomali bildirimi | Kaçırılan hatırlatma veya olağan dışı durumda uyarı |
| Yakın paneli | Bakıcı ve aile yakınlarının takip ekranı |
| Acil buton | Tek tıkla yakınlara acil durum bildirimi |

## Teknolojiler

- C# / .NET 10
- ASP.NET Core Razor Pages
- Entity Framework Core 10 (SQL Server LocalDB)
- Bootstrap 5

## Mimari

Katmanlı mimari: **Sunum** (`yaslibakimapp`) → **İş** (`YasliBakim.Business`) → **Veri Erişim** (`YasliBakim.DataAccess`), ortak varlıklar `YasliBakim.Entities` içinde.

- Mimari şema ve klasör yapısı: [docs/Mimari.md](docs/Mimari.md)
- ER diyagramı ve ilişkiler: [docs/ER-Diyagrami.md](docs/ER-Diyagrami.md)
- Veritabanı, kısıtlar, indeksler ve CRUD: [docs/Veritabani.md](docs/Veritabani.md)
- Kimlik doğrulama, roller ve güvenlik notu: [docs/Guvenlik.md](docs/Guvenlik.md)
- İlaç/Aktivite Hatırlatma modülü: [docs/Hatirlatma.md](docs/Hatirlatma.md)

## Kurulum ve çalıştırma

Gereksinimler: [.NET 10 SDK](https://dotnet.microsoft.com/download), SQL Server LocalDB (Visual Studio ile gelir).

```bash
git clone <depo-adresi>
cd <depo-klasoru>
dotnet build YasliBakim.slnx
dotnet run --project yaslibakimapp
```

Bağlantı dizesi `yaslibakimapp/appsettings.json` içindeki `DefaultConnection` alanındadır. Uygulama Development ortamında ilk açılışta veritabanını migration ile oluşturur ve sentetik örnek veriyi yükler.

**Demo hesapları** (yalnızca geliştirme ortamında oluşturulur, parola hepsinde `Demo1234`):
`yonetici@ornek.test` (Yönetici), `selin@ornek.test` (Bakıcı), `ayse@ornek.test` (Yaşlı). Tam liste: [docs/Guvenlik.md](docs/Guvenlik.md#demo-hesapları-yalnızca-geliştirme-ortamı).

```bash
dotnet test YasliBakim.slnx      # CRUD, kısıt, parola hash ve rol erişim testleri
sqlcmd -S "(localdb)\MSSQLLocalDB" -d YasliBakimDb -i database/02_ornek_sorgular.sql   # örnek sorgular
```

## Yol haritası

| Hafta | Konu | Durum |
|---|---|---|
| 3 | Kurulum, mimari ve veri modeli | ✅ |
| 4 | Veritabanı tasarımı ve veri erişim katmanı | ✅ |
| 5 | Kullanıcı yönetimi ve kimlik doğrulama | ✅ |
| 6 | Çekirdek Modül A: İlaç/Aktivite Hatırlatma | ✅ |
| 7–15 | Modüllerin geliştirilmesi | ⏳ |
