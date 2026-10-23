-- =====================================================================
-- Örnek sorgular (SQL Server / LocalDB, veritabanı: YasliBakimDb)
-- Çalıştırma: sqlcmd -S "(localdb)\MSSQLLocalDB" -d YasliBakimDb -i database\02_ornek_sorgular.sql
-- Örnek veri, uygulama Development ortamında ilk açıldığında OrnekVeri.YukleAsync ile yüklenir.
-- =====================================================================
SET NOCOUNT ON;
-- Olaylar tablosundaki filtreli UNIQUE indeks (IX_Olaylar_AktiviteKaydiId) için gerekli; sqlcmd varsayılanı OFF.
SET QUOTED_IDENTIFIER ON;

-- 1) N-N: Her yaşlının bakıcıları (ara tablo YasliBakicilar üzerinden iki JOIN)
PRINT N'--- 1) Yaşlı - bakıcı eşleştirmeleri';
SELECT y.Ad + ' ' + y.Soyad   AS Yasli,
       b.Ad + ' ' + b.Soyad   AS Bakici,
       CASE b.Rol WHEN 1 THEN 'Profesyonel' ELSE 'Aile yakını' END AS Rol,
       yb.YakinlikDerecesi,
       yb.BirincilSorumluMu
FROM Yaslilar y
JOIN YasliBakicilar yb ON yb.YasliId = y.Id
JOIN Bakicilar b       ON b.Id = yb.BakiciId
ORDER BY y.Soyad, yb.BirincilSorumluMu DESC;

-- 2) Bugün geçerli aktif hatırlatmalar (IX_Hatirlatma_Yasli_Aktif_Saat indeksini kullanır)
PRINT N'--- 2) Bugünkü hatırlatmalar';
DECLARE @bugun date = CAST(GETDATE() AS date);
SELECT y.Ad + ' ' + y.Soyad AS Yasli, h.Saat, h.Baslik
FROM Hatirlatmalar h
JOIN Yaslilar y ON y.Id = h.YasliId
WHERE h.AktifMi = 1
  AND h.BaslangicTarihi <= @bugun
  AND (h.BitisTarihi IS NULL OR h.BitisTarihi >= @bugun)
ORDER BY y.Soyad, h.Saat;

-- 3) Açık olaylar: önce en kritik (IX_Olay_Durum_Seviye)
PRINT N'--- 3) Açık olaylar';
SELECT o.Id, o.Seviye, o.Tur, y.Ad + ' ' + y.Soyad AS Yasli, o.Aciklama,
       ISNULL(b.Ad + ' ' + b.Soyad, '-') AS Ilgilenen, o.OlusmaZamani
FROM Olaylar o
JOIN Yaslilar y       ON y.Id = o.YasliId
LEFT JOIN Bakicilar b ON b.Id = o.IlgilenenBakiciId
WHERE o.Durum <> 3
ORDER BY o.Seviye DESC, o.OlusmaZamani DESC;

-- 4) Son 7 günde ilaç uyumu (GROUP BY + koşullu sayım)
PRINT N'--- 4) İlaç uyum oranı (son 7 gün)';
SELECT y.Ad + ' ' + y.Soyad AS Yasli,
       COUNT(*)                                        AS ToplamOnay,
       SUM(CASE WHEN a.Durum = 1 THEN 1 ELSE 0 END)    AS Tamamlanan,
       CAST(100.0 * SUM(CASE WHEN a.Durum = 1 THEN 1 ELSE 0 END) / COUNT(*) AS decimal(5,1)) AS UyumYuzde
FROM AktiviteKayitlari a
JOIN Yaslilar y     ON y.Id = a.YasliId
JOIN Hatirlatmalar h ON h.Id = a.HatirlatmaId
WHERE a.Tur = 1 AND h.Tur = 1
  AND a.KayitZamani >= DATEADD(day, -7, GETDATE())
GROUP BY y.Id, y.Ad, y.Soyad
ORDER BY UyumYuzde;

-- 5) CRUD örneği — tek transaction içinde, sonunda ROLLBACK ile geri alınır (veri bozulmaz)
PRINT N'--- 5) CRUD (transaction, sonunda geri alınır)';
BEGIN TRANSACTION;

    -- CREATE
    INSERT INTO Yaslilar (Ad, Soyad, DogumTarihi, Telefon, OlusturmaTarihi)
    VALUES (N'Deneme', N'Kişi', '1950-05-05', '0500 000 99 99', GETDATE());
    DECLARE @yeniId int = SCOPE_IDENTITY();

    -- READ
    SELECT Id, Ad, Soyad, Telefon FROM Yaslilar WHERE Id = @yeniId;

    -- UPDATE
    UPDATE Yaslilar SET Telefon = '0500 000 88 88' WHERE Id = @yeniId;
    SELECT Id, Telefon AS GuncelTelefon FROM Yaslilar WHERE Id = @yeniId;

    -- DELETE
    DELETE FROM Yaslilar WHERE Id = @yeniId;
    SELECT COUNT(*) AS SilindiktenSonraKalan FROM Yaslilar WHERE Id = @yeniId;

ROLLBACK TRANSACTION;

-- 6) Kısıt denemeleri — hepsi hata vermelidir
PRINT N'--- 6) Kısıt denemeleri (hata beklenir)';
BEGIN TRY
    INSERT INTO Hatirlatmalar (YasliId, Baslik, Tur, Saat, Tekrar, BaslangicTarihi, AktifMi, OlusturmaTarihi)
    VALUES (99999, N'Yok', 1, '09:00', 2, '2026-01-01', 1, GETDATE());
END TRY
BEGIN CATCH PRINT N'FK hatası (beklenen): ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    INSERT INTO Hatirlatmalar (YasliId, Baslik, Tur, Saat, Tekrar, BaslangicTarihi, BitisTarihi, AktifMi, OlusturmaTarihi)
    SELECT TOP 1 Id, N'Ters tarih', 1, '09:00', 2, '2026-02-01', '2026-01-01', 1, GETDATE() FROM Yaslilar;
END TRY
BEGIN CATCH PRINT N'CHECK hatası (beklenen): ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    INSERT INTO Bakicilar (Ad, Soyad, Eposta, Telefon, Rol, OlusturmaTarihi)
    VALUES (N'Kopya', N'Kişi', 'elif@ornek.test', '0500 000 00 00', 2, GETDATE());
END TRY
BEGIN CATCH PRINT N'UNIQUE hatası (beklenen): ' + ERROR_MESSAGE(); END CATCH;
