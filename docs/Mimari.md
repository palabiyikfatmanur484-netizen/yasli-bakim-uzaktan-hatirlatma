# Mimari Şema

Proje **katmanlı mimari** (Sunum / İş / Veri) ile kurulmuştur. Her katman ayrı bir C# projesidir. Bağımlılıklar yalnızca yukarıdan aşağıya doğrudur.

```mermaid
flowchart TB
    U([Kullanıcı: Yaşlı / Bakıcı / Yakın]) --> P

    subgraph P["Sunum Katmanı — yaslibakimapp (ASP.NET Core Razor Pages)"]
        P1[Pages/*.cshtml + PageModel]
        P2[wwwroot: css, js, bootstrap]
        P3[Program.cs: DI kayıtları]
    end

    subgraph B["İş Katmanı — YasliBakim.Business"]
        B1[Abstract: IYasliService ...]
        B2[Concrete: YasliManager ...<br/>iş kuralları, doğrulama]
    end

    subgraph D["Veri Erişim Katmanı — YasliBakim.DataAccess"]
        D1[Abstract: IGenericRepository&lt;T&gt;]
        D2[Concrete: EfGenericRepository&lt;T&gt;]
        D3[Context: YasliBakimDbContext]
    end

    subgraph E["Varlık Katmanı — YasliBakim.Entities"]
        E1[Concrete: Yasli, Bakici, Hatirlatma,<br/>AktiviteKaydi, Olay, YasliBakici]
        E2[Enums: HatirlatmaTuru, OlayTuru ...]
    end

    DB[(SQL Server<br/>YasliBakimDb)]

    P --> B
    B --> D
    D --> DB
    P -.-> E
    B -.-> E
    D -.-> E
```

## Klasör yapısı

```
YasliBakim.slnx
├── YasliBakim.Entities/        Varlık sınıfları (veritabanı tabloları)
│   ├── Concrete/               BaseEntity, Yasli, Bakici, Hatirlatma ...
│   └── Enums/                  Sabit değer listeleri
├── YasliBakim.DataAccess/      Veritabanı erişimi (EF Core)
│   ├── Abstract/               Repository arayüzleri
│   ├── Concrete/               EF Core implementasyonları
│   ├── Context/                DbContext, ilişkiler, kısıtlar, indeksler (Fluent API)
│   ├── Migrations/             EF Core migration'ları
│   └── Seed/                   Sentetik örnek veri
├── YasliBakim.Business/        İş kuralları
│   ├── Abstract/               Servis arayüzleri
│   └── Concrete/               Servis implementasyonları (Manager)
├── yaslibakimapp/              Sunum katmanı (Razor Pages web uygulaması)
├── YasliBakim.Tests/           CRUD ve kısıt testleri (xUnit + SQLite in-memory)
├── database/                   Şema script'i ve örnek SQL sorguları
└── docs/                       ER diyagramı, mimari şema, veritabanı tasarımı
```

## Gerekçeler

- **Sorumlulukların ayrılması:** Sayfalar (Razor Pages) veritabanını doğrudan bilmez; yalnızca servis arayüzlerini çağırır. Veritabanı değişirse (ör. SQL Server → SQLite) sadece DataAccess katmanı değişir.
- **Abstract / Concrete ayrımı:** Üst katmanlar arayüze bağımlıdır, somut sınıf `Program.cs` içinde Dependency Injection ile verilir. Bu sayede test için sahte (mock) sınıflar kullanılabilir.
- **Ayrı Entities projesi:** Varlıklar tüm katmanlarca kullanılır, ancak hiçbir katmana bağımlı değildir. Bu döngüsel bağımlılığı önler.
- **Generic repository:** Altı varlık için aynı CRUD kodunu tekrar yazmamak için kullanılır.

## Bağımlılık yönü

| Proje | Referans verdiği projeler |
|---|---|
| YasliBakim.Entities | — |
| YasliBakim.DataAccess | Entities |
| YasliBakim.Business | Entities, DataAccess |
| yaslibakimapp | Entities, DataAccess, Business |
