# Order Saga — Step 3 + 4 deploy riskleri

Kapsam: Order servisinin consumer tabanlı yapıdan `OrderStateMachine` saga'sına geçişi (Step 3), pessimistic saga
repository (Step 3.1) ve saga içi ödeme timeout'u (Step 4). `OrderTimeoutWorker` kaldırıldı.

## 1. Ön koşul: RabbitMQ delayed exchange plugin'i (şu an YOK)

- `rabbitmq:3.13-management-alpine` image'ında `rabbitmq_delayed_message_exchange` bulunmuyor; `enabled_plugins`
  yalnızca `rabbitmq_management, rabbitmq_prometheus` (2026-11-01'de image üzerinden kontrol edildi).
- Gerekenler: 3.13 ile uyumlu plugin `.ez` dosyası (community plugin, v3.13.x) image'a eklenmeli (custom Dockerfile
  veya `/opt/rabbitmq/plugins` mount) ve `rabbitmq-plugins enable rabbitmq_delayed_message_exchange` ile etkinleştirilmeli
  (veya `enabled_plugins` dosyasına eklenmeli). Bu bir altyapı değişikliğidir, henüz yapılmadı.
- Plugin olmadan: Order, zamanlanmış mesaj için `x-delayed-message` tipinde exchange tanımlamaya çalışır, broker reddeder.
  Timeout mesajı hiç teslim edilmez, siparişler `Beklemede` kalır. Outbox teslimatı bu mesajda hata alıp tekrar dener;
  aynı outbox'taki diğer mesajların gecikip gecikmeyeceği doğrulanmadı.
- Plugin'in bilinen sınırları: gecikmiş mesajlar tek node'da tutulur (replike edilmez), iptal edilemez.
  İptal edilemeyen mesajlar saga'da token kontrolü (`PaymentTimeoutTokenId`) ve terminal durumlardaki `Ignore` ile etkisizdir.

## 2. Deploy sırası

1. RabbitMQ: plugin'i etkinleştir (madde 1).
2. **Stock** servisini deploy et (`ReleaseStockCommandConsumer`). Order'dan önce olmalı: Stock'un `release-stock-command`
   kuyruğu yokken publish edilen `ReleaseStockCommand` hiçbir kuyruğa yönlenmez ve kaybolur. Sonuç: stok iade edilmez.
3. Eski Order'ın kuyruklarını boşalt (madde 3), ardından **Order** servisini deploy et.

Step 3 tek başına deploy edilmemeli. Step 3'te oluşan saga instance'larında `ExpiresAt` ve zamanlanmış timeout yok.
Step 4'ten sonra bu instance'lar için timeout hiç gelmez (madde 4).

## 3. Eski kuyruklar

- Step 3 öncesinin kuyrukları `order-stock-failed-queue`, `order-payment-failed-queue`, `order-payment-completed-queue`
  (ve `_error` / `_skipped` eşleri) event exchange'lerine bağlı kalır. Artık tüketicileri yok, mesaj birikmeye devam eder.
- Deploy'dan önce eski sürüm bu kuyrukları boşaltmalı. Kuyrukta kalan son event (örn. `PaymentCompleted`) saga'ya
  ulaşmaz ve sipariş takılı kalır. Deploy'dan sonra kuyruklar boş olduğu doğrulanarak management UI'dan silinmeli.
- Yeni kuyruklar: Order'da `order-saga`, Stock'ta `release-stock-command`.

## 4. Eski siparişler

| Durum | Ne olur | Öneri |
|---|---|---|
| Deploy anında `Beklemede` olan, saga instance'ı olmayan sipariş | Stock/Payment event'leri `OnMissingInstance → Fault` ile `order-saga_error`'a düşer. Worker kaldırıldığı için 15 dk sonra iptal de edilmez | Step 7'ye kadar: deploy'u bekleyen sipariş yokken yap veya bu siparişleri deploy öncesi manuel iptal et / tamamla |
| Saga instance'ı var ama `ExpiresAt` boş (yalnızca Step 3 ayrı deploy edilirse) | Zamanlanmış timeout yok, `AwaitingStock/AwaitingPayment`'ta kalır | Step 3 ile 4'ü birlikte deploy et |

## 5. Açık kalan riskler

- **Kaçan timeout için emniyet ağı yok.** Delayed mesaj kaybolursa (RabbitMQ node kaybı) sipariş `Beklemede` ve stok
  rezerve kalır. `ExpiresAt` ve `(CurrentState, ExpiresAt)` index'i hazır. Süresi geçmiş instance'lara saga üzerinden
  `PaymentTimeoutExpired` gönderen bir sweeper eklenebilir. Bu sweeper Order tablosuna doğrudan yazmamalı: eski worker'ın
  "saga Completed / DB İptal" tutarsızlığı böyle oluşuyordu.
- **Timeout'tan sonra gelen ödeme:** Step 5'e kadar `Canceled` durumunda `PaymentCompleted` yoksayılır. Para çekilmiş,
  sipariş iptal kalır, iade yapılmaz.
- **Pessimistic kilit:** MassTransit 8.0.16'nın lock statement'ı `WITH (UPDLOCK, ROWLOCK, SERIALIZABLE)` üretir. Henüz var
  olmayan instance için key-range kilidi alınır: eşzamanlı kopya `OrderCreated`'da PK ihlali oluşmaz, ikinci mesaj bekler.
  Aynı key aralığına düşen farklı yeni siparişler de kısa süre birbirini bekleyebilir (CorrelationId rastgele GUID olduğu
  için etkisi düşük). Bekleme komut timeout'u (30 sn) ile sınırlıdır.
- **`Canceled` + geç `StockReserved`:** Timeout stok cevabından önce gelirse saga yeniden `ReleaseStockCommand` publish eder.
  Stock iadesi siparişin aktif rezervasyonlarına göre yapıldığı için tekrar zararsızdır.
- **Test:** factory'deki 3 sn'lik global timeout tüm saga testlerinde çalışır. `AwaitingPayment`'ta ~2 sn bekleyen
  testlerin payı ~1 sn; yavaş CI'da flaky olabilir.
- `OrderState.RowVersion` kolonu artık kullanılmıyor. Kaldırılması ayrı bir migration gerektirir.
