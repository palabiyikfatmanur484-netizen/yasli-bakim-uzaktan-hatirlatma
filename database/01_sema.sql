IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE TABLE [Bakicilar] (
        [Id] int NOT NULL IDENTITY,
        [Ad] nvarchar(50) NOT NULL,
        [Soyad] nvarchar(50) NOT NULL,
        [Eposta] nvarchar(100) NOT NULL,
        [Telefon] nvarchar(20) NOT NULL,
        [Rol] int NOT NULL,
        [OlusturmaTarihi] datetime2 NOT NULL,
        CONSTRAINT [PK_Bakicilar] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE TABLE [Yaslilar] (
        [Id] int NOT NULL IDENTITY,
        [Ad] nvarchar(50) NOT NULL,
        [Soyad] nvarchar(50) NOT NULL,
        [DogumTarihi] date NOT NULL,
        [Telefon] nvarchar(20) NULL,
        [Adres] nvarchar(250) NULL,
        [KronikHastaliklar] nvarchar(500) NULL,
        [AcilDurumNotu] nvarchar(500) NULL,
        [OlusturmaTarihi] datetime2 NOT NULL,
        CONSTRAINT [PK_Yaslilar] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Yasli_DogumTarihi] CHECK ([DogumTarihi] >= '1900-01-01')
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE TABLE [Hatirlatmalar] (
        [Id] int NOT NULL IDENTITY,
        [YasliId] int NOT NULL,
        [OlusturanBakiciId] int NULL,
        [Baslik] nvarchar(100) NOT NULL,
        [Aciklama] nvarchar(500) NULL,
        [Tur] int NOT NULL,
        [Saat] time NOT NULL,
        [Tekrar] int NOT NULL,
        [BaslangicTarihi] date NOT NULL,
        [BitisTarihi] date NULL,
        [AktifMi] bit NOT NULL,
        [OlusturmaTarihi] datetime2 NOT NULL,
        CONSTRAINT [PK_Hatirlatmalar] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Hatirlatma_Tarih] CHECK ([BitisTarihi] IS NULL OR [BitisTarihi] >= [BaslangicTarihi]),
        CONSTRAINT [FK_Hatirlatmalar_Bakicilar_OlusturanBakiciId] FOREIGN KEY ([OlusturanBakiciId]) REFERENCES [Bakicilar] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Hatirlatmalar_Yaslilar_YasliId] FOREIGN KEY ([YasliId]) REFERENCES [Yaslilar] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE TABLE [YasliBakicilar] (
        [YasliId] int NOT NULL,
        [BakiciId] int NOT NULL,
        [YakinlikDerecesi] nvarchar(30) NULL,
        [BirincilSorumluMu] bit NOT NULL,
        [AtanmaTarihi] datetime2 NOT NULL,
        CONSTRAINT [PK_YasliBakicilar] PRIMARY KEY ([YasliId], [BakiciId]),
        CONSTRAINT [FK_YasliBakicilar_Bakicilar_BakiciId] FOREIGN KEY ([BakiciId]) REFERENCES [Bakicilar] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_YasliBakicilar_Yaslilar_YasliId] FOREIGN KEY ([YasliId]) REFERENCES [Yaslilar] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE TABLE [AktiviteKayitlari] (
        [Id] int NOT NULL IDENTITY,
        [YasliId] int NOT NULL,
        [HatirlatmaId] int NULL,
        [Tur] int NOT NULL,
        [KayitZamani] datetime2 NOT NULL,
        [Durum] int NULL,
        [RuhHali] int NULL,
        [Not] nvarchar(500) NULL,
        [OlusturmaTarihi] datetime2 NOT NULL,
        CONSTRAINT [PK_AktiviteKayitlari] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AktiviteKaydi_Tur] CHECK (([Tur] = 1 AND [HatirlatmaId] IS NOT NULL AND [Durum] IS NOT NULL) OR ([Tur] = 2 AND [HatirlatmaId] IS NULL AND [RuhHali] IS NOT NULL)),
        CONSTRAINT [FK_AktiviteKayitlari_Hatirlatmalar_HatirlatmaId] FOREIGN KEY ([HatirlatmaId]) REFERENCES [Hatirlatmalar] ([Id]),
        CONSTRAINT [FK_AktiviteKayitlari_Yaslilar_YasliId] FOREIGN KEY ([YasliId]) REFERENCES [Yaslilar] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE TABLE [Olaylar] (
        [Id] int NOT NULL IDENTITY,
        [YasliId] int NOT NULL,
        [AktiviteKaydiId] int NULL,
        [IlgilenenBakiciId] int NULL,
        [Tur] int NOT NULL,
        [Seviye] int NOT NULL,
        [Aciklama] nvarchar(500) NULL,
        [OlusmaZamani] datetime2 NOT NULL,
        [Durum] int NOT NULL,
        [CozulmeZamani] datetime2 NULL,
        [OlusturmaTarihi] datetime2 NOT NULL,
        CONSTRAINT [PK_Olaylar] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Olay_CozulmeZamani] CHECK ([CozulmeZamani] IS NULL OR [CozulmeZamani] >= [OlusmaZamani]),
        CONSTRAINT [FK_Olaylar_AktiviteKayitlari_AktiviteKaydiId] FOREIGN KEY ([AktiviteKaydiId]) REFERENCES [AktiviteKayitlari] ([Id]),
        CONSTRAINT [FK_Olaylar_Bakicilar_IlgilenenBakiciId] FOREIGN KEY ([IlgilenenBakiciId]) REFERENCES [Bakicilar] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Olaylar_Yaslilar_YasliId] FOREIGN KEY ([YasliId]) REFERENCES [Yaslilar] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_AktiviteKaydi_Yasli_Zaman] ON [AktiviteKayitlari] ([YasliId], [KayitZamani]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_AktiviteKayitlari_HatirlatmaId] ON [AktiviteKayitlari] ([HatirlatmaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Bakici_Eposta] ON [Bakicilar] ([Eposta]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_Hatirlatma_Yasli_Aktif_Saat] ON [Hatirlatmalar] ([YasliId], [AktifMi], [Saat]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_Hatirlatmalar_OlusturanBakiciId] ON [Hatirlatmalar] ([OlusturanBakiciId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_Olay_Durum_Seviye] ON [Olaylar] ([Durum], [Seviye]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_Olay_Yasli_Zaman] ON [Olaylar] ([YasliId], [OlusmaZamani]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Olaylar_AktiviteKaydiId] ON [Olaylar] ([AktiviteKaydiId]) WHERE [AktiviteKaydiId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_Olaylar_IlgilenenBakiciId] ON [Olaylar] ([IlgilenenBakiciId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_YasliBakici_BakiciId] ON [YasliBakicilar] ([BakiciId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    CREATE INDEX [IX_Yasli_Soyad_Ad] ON [Yaslilar] ([Soyad], [Ad]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023065717_IlkSema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023065717_IlkSema', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023073856_KullaniciTablosu'
)
BEGIN
    CREATE TABLE [Kullanicilar] (
        [Id] int NOT NULL IDENTITY,
        [Eposta] nvarchar(100) NOT NULL,
        [ParolaHash] nvarchar(200) NOT NULL,
        [Rol] int NOT NULL,
        [BakiciId] int NULL,
        [YasliId] int NULL,
        [AktifMi] bit NOT NULL,
        [BasarisizGirisSayisi] int NOT NULL,
        [KilitBitisZamani] datetime2 NULL,
        [SonGirisZamani] datetime2 NULL,
        [OlusturmaTarihi] datetime2 NOT NULL,
        CONSTRAINT [PK_Kullanicilar] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Kullanici_Rol] CHECK (([Rol] = 1 AND [BakiciId] IS NULL AND [YasliId] IS NULL) OR ([Rol] = 2 AND [BakiciId] IS NOT NULL AND [YasliId] IS NULL) OR ([Rol] = 3 AND [YasliId] IS NOT NULL AND [BakiciId] IS NULL)),
        CONSTRAINT [FK_Kullanicilar_Bakicilar_BakiciId] FOREIGN KEY ([BakiciId]) REFERENCES [Bakicilar] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Kullanicilar_Yaslilar_YasliId] FOREIGN KEY ([YasliId]) REFERENCES [Yaslilar] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023073856_KullaniciTablosu'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Kullanicilar_BakiciId] ON [Kullanicilar] ([BakiciId]) WHERE [BakiciId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023073856_KullaniciTablosu'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Kullanicilar_YasliId] ON [Kullanicilar] ([YasliId]) WHERE [YasliId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023073856_KullaniciTablosu'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Kullanici_Eposta] ON [Kullanicilar] ([Eposta]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023073856_KullaniciTablosu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023073856_KullaniciTablosu', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030080342_HatirlatmaTakibi'
)
BEGIN
    DROP INDEX [IX_AktiviteKayitlari_HatirlatmaId] ON [AktiviteKayitlari];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030080342_HatirlatmaTakibi'
)
BEGIN
    ALTER TABLE [AktiviteKayitlari] DROP CONSTRAINT [CK_AktiviteKaydi_Tur];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030080342_HatirlatmaTakibi'
)
BEGIN
    ALTER TABLE [AktiviteKayitlari] ADD [PlanlananZaman] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030080342_HatirlatmaTakibi'
)
BEGIN
    UPDATE a
    SET a.PlanlananZaman = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00' AS time), h.Saat),
                                   CAST(CAST(a.KayitZamani AS date) AS datetime2))
    FROM AktiviteKayitlari a
    JOIN Hatirlatmalar h ON h.Id = a.HatirlatmaId
    WHERE a.Tur = 1 AND a.PlanlananZaman IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030080342_HatirlatmaTakibi'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_AktiviteKaydi_Hatirlatma_Planlanan] ON [AktiviteKayitlari] ([HatirlatmaId], [PlanlananZaman]) WHERE [HatirlatmaId] IS NOT NULL AND [PlanlananZaman] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030080342_HatirlatmaTakibi'
)
BEGIN
    EXEC(N'ALTER TABLE [AktiviteKayitlari] ADD CONSTRAINT [CK_AktiviteKaydi_Tur] CHECK (([Tur] = 1 AND [HatirlatmaId] IS NOT NULL AND [Durum] IS NOT NULL AND [PlanlananZaman] IS NOT NULL) OR ([Tur] = 2 AND [HatirlatmaId] IS NULL AND [RuhHali] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030080342_HatirlatmaTakibi'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030080342_HatirlatmaTakibi', N'10.0.13');
END;

COMMIT;
GO

