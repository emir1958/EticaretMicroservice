/*
  Tek seferlik: saga instance'ı olmayan, beklemede kalmış siparişler (Order saga'ya geçiş öncesi oluşmuş).

  Neden: Step 4 ile OrderTimeoutWorker kaldırıldı. OrderTimeoutSweeper yalnızca saga instance'ı olan siparişlere bakar.
  Saga öncesinden kalan bekleyen siparişleri artık hiçbir şey iptal etmez. Bu siparişlere gelen Stock/Payment event'leri de
  OnMissingInstance -> Fault ile order-saga_error kuyruğuna düşer.

  Ne zaman: Order deploy'undan hemen önce ya da sonra, şu koşullar sağlandıktan sonra:
    - Eski Order sürümü durdurulmuş ve order-*-queue kuyrukları boşalmış olmalı.
    - Stock'un order-created-event kuyruğu boş olmalı. Aksi halde iptal edilen bir sipariş için sonradan rezervasyon yapılabilir.
  Ayrıntı: step4-deploy-risks.md, madde 4.

  Kullanım:
    1. @Cancel = 0 ile çalıştır (varsayılan): yalnızca listeler, hiçbir şey değiştirmez.
    2. Listeyi gözden geçir. Ödemesi alınmış siparişler (ikinci sonuç kümesi) İPTAL EDİLMEZ, manuel karar gerektirir
       (tamamlama ya da iade).
    3. @Cancel = 1 ile çalıştır: siparişler iptal edilir ve aktif stok rezervasyonları iade edilir (tek transaction).

  Kapsam dışı: müşteriye SignalR bildirimi gönderilmez. Payment'ta iade yapılmaz (ödemesi alınmış siparişler zaten hariç tutulur).

  Veritabanları aynı SQL Server instance'ında: OrderDb (ordering şeması), StockDb (dbo), PaymentDb (dbo).
  Enum değerleri: OrderStatus Beklemede=1, IptalEdildi=3 | StockReservationStatus Reserved=1, Released=3 | PaymentStatus Success=1
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Cancel bit = 0;              -- 0: yalnızca listele, 1: iptal et + stok iade
DECLARE @OlderThanMinutes int = 15;   -- Henüz süreci devam ediyor olabilecek yeni siparişlere dokunma

IF OBJECT_ID('tempdb..#Targets') IS NOT NULL DROP TABLE #Targets;

SELECT
    o.Id            AS OrderId,
    o.CorrelationId,
    o.BuyerId,
    o.CreatedDate,
    CAST(CASE WHEN EXISTS (SELECT 1 FROM PaymentDb.dbo.Payments p WHERE p.OrderId = o.Id AND p.Status = 1)
              THEN 1 ELSE 0 END AS bit) AS IsPaid
INTO #Targets
FROM OrderDb.ordering.Orders o
WHERE o.OrderStatus = 1
  AND o.CreatedDate < DATEADD(MINUTE, -@OlderThanMinutes, SYSUTCDATETIME())
  AND NOT EXISTS (SELECT 1 FROM OrderDb.ordering.OrderStates s WHERE s.CorrelationId = o.CorrelationId);

-- 1. İptal adayları (ödemesi alınmamış) ve iade edilecek rezervasyonlar
SELECT t.OrderId, t.CorrelationId, t.BuyerId, t.CreatedDate,
       r.ReservedLines, r.ReservedQuantity
FROM #Targets t
OUTER APPLY (
    SELECT COUNT(*) AS ReservedLines, SUM(sr.Quantity) AS ReservedQuantity
    FROM StockDb.dbo.StockReservations sr
    WHERE sr.OrderId = t.OrderId AND sr.Status = 1
) r
WHERE t.IsPaid = 0
ORDER BY t.CreatedDate;

-- 2. Ödemesi alınmış ama beklemede kalmış siparişler: bu script dokunmaz, manuel karar gerekir
SELECT t.OrderId, t.CorrelationId, t.BuyerId, t.CreatedDate, p.TotalPrice, p.CreatedAt AS PaidAt
FROM #Targets t
JOIN PaymentDb.dbo.Payments p ON p.OrderId = t.OrderId AND p.Status = 1
WHERE t.IsPaid = 1
ORDER BY t.CreatedDate;

IF @Cancel = 1
BEGIN
    IF OBJECT_ID('tempdb..#Canceled') IS NOT NULL DROP TABLE #Canceled;
    CREATE TABLE #Canceled (OrderId int PRIMARY KEY);

    BEGIN TRANSACTION;

    -- Koşullar kilit altında yeniden kontrol edilir: liste alındıktan sonra durumu değişen sipariş iptal edilmez
    UPDATE o
    SET o.OrderStatus = 3
    OUTPUT inserted.Id INTO #Canceled (OrderId)
    FROM OrderDb.ordering.Orders o WITH (UPDLOCK, ROWLOCK)
    JOIN #Targets t ON t.OrderId = o.Id AND t.IsPaid = 0
    WHERE o.OrderStatus = 1
      AND NOT EXISTS (SELECT 1 FROM OrderDb.ordering.OrderStates s WHERE s.CorrelationId = o.CorrelationId)
      AND NOT EXISTS (SELECT 1 FROM PaymentDb.dbo.Payments p WHERE p.OrderId = o.Id AND p.Status = 1);

    -- StockService.ReleaseForOrderAsync ile aynı kural: yalnızca bu siparişlerin aktif (Reserved) rezervasyonları iade edilir
    IF OBJECT_ID('tempdb..#Released') IS NOT NULL DROP TABLE #Released;

    SELECT sr.Id, sr.ProductId, sr.Quantity
    INTO #Released
    FROM StockDb.dbo.StockReservations sr WITH (UPDLOCK, ROWLOCK)
    JOIN #Canceled c ON c.OrderId = sr.OrderId
    WHERE sr.Status = 1;

    UPDATE ps
    SET ps.AvailableStock = ps.AvailableStock + x.Quantity,
        ps.ReservedStock  = ps.ReservedStock  - x.Quantity
    FROM StockDb.dbo.ProductStocks ps
    JOIN (SELECT ProductId, SUM(Quantity) AS Quantity FROM #Released GROUP BY ProductId) x ON x.ProductId = ps.ProductId;

    UPDATE sr
    SET sr.Status = 3,
        sr.UpdatedAt = SYSUTCDATETIME()
    FROM StockDb.dbo.StockReservations sr
    JOIN #Released r ON r.Id = sr.Id;

    COMMIT TRANSACTION;

    SELECT (SELECT COUNT(*) FROM #Canceled) AS CanceledOrders,
           (SELECT COUNT(*) FROM #Released) AS ReleasedReservationLines,
           (SELECT ISNULL(SUM(Quantity), 0) FROM #Released) AS ReleasedQuantity;
END
