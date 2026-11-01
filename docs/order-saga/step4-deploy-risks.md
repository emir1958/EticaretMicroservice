# Order Saga — Step 3 + 4 deploy riskleri

Kapsam: Order servisinin consumer tabanlı yapıdan `OrderStateMachine` saga'sına geçişi (Step 3), pessimistic saga
repository (Step 3.1) ve saga içi ödeme timeout'u (Step 4). `OrderTimeoutWorker` kaldırıldı.

## 1. Ön koşul: RabbitMQ delayed exchange plugin'i

- Order, `UseDelayedMessageScheduler()` kullanır; broker'da `rabbitmq_delayed_message_exchange` etkin olmalıdır.
  Düz `rabbitmq:3.13-management-alpine` image'ında bu plugin yoktur.
- Çözüm: [infra/rabbitmq/Dockerfile](../../infra/rabbitmq/Dockerfile). Aynı base image'ı (`rabbitmq:3.13-management-alpine`,
  build anında 3.13.7) kullanır. Plugin **v3.13.0** sürümüne sabitlenmiştir ve SHA-256
  `3479180df03fa830fd59ccf638a60eee8f0e8261524a8616685522d7b8d9a9ee` ile doğrulanır. Plugin build sırasında
  `rabbitmq-plugins enable --offline` ile etkinleştirilir.
  docker-compose `rabbitmq` servisi artık bu Dockerfile'dan `eticaret-rabbitmq:3.13-delayed` olarak build edilir.
  Container adı, port'lar, volume ve kimlik bilgileri değişmedi.
- Doğrulama (2026-11-01, geçici container): `rabbitmq-plugins list -e` → `[E*] rabbitmq_delayed_message_exchange 3.13.0`.
  `x-delayed-message` tipinde exchange tanımlanabildi.
- `rabbitmq_bus` container'ı yeni image ile **yeniden oluşturulmalıdır** (`docker compose up -d --build rabbitmq`).
  Eski container plugin'siz kalır. `enabled_plugins` dosyası `/etc/rabbitmq` altında durur, `rabbitmq_data` volume'u etkilenmez.
- Base image tag'i (`3.13-management-alpine`) kayan bir tag'dir. 3.13 serisi içinde güncellenmesi plugin ile uyumludur;
  minor sürüm (örn. 4.x) değişirse plugin sürümü de değiştirilmelidir.
- Plugin olmadan: Order, zamanlanmış mesaj için `x-delayed-message` tipinde exchange tanımlamaya çalışır, broker reddeder.
  Timeout mesajı hiç teslim edilmez, siparişler `Beklemede` kalır. Outbox teslimatı bu mesajda hata alıp tekrar dener;
  aynı outbox'taki diğer mesajların gecikip gecikmeyeceği doğrulanmadı.
- Plugin'in bilinen sınırları: gecikmiş mesajlar tek node'da tutulur (replike edilmez), iptal edilemez.
  İptal edilemeyen mesajlar saga'da token kontrolü (`PaymentTimeoutTokenId`) ve terminal durumlardaki `Ignore` ile etkisizdir.

## 2. Deploy sırası

1. RabbitMQ: `rabbitmq_bus`'ı yeni image ile yeniden oluştur (madde 1).
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

Script varsayılan olarak yalnızca listeler (`@Cancel = 0`). `@Cancel = 1` ile ödemesi alınmamış siparişleri iptal eder
ve aktif stok rezervasyonlarını aynı transaction'da iade eder. Ödemesi alınmış ama beklemede kalmış siparişlere dokunmaz;
bunları ayrı listeler ve manuel karara bırakır. Müşteriye bildirim gönderilmez.

| Durum | Ne olur | Öneri |
|---|---|---|
| Deploy anında `Beklemede` olan, saga instance'ı olmayan sipariş | Stock/Payment event'leri `OnMissingInstance → Fault` ile `order-saga_error`'a düşer. Worker kaldırıldığı için 15 dk sonra iptal de edilmez. Sweeper bunları seçmez (yalnızca saga instance'larına bakar) | Bekleyen sipariş yokken deploy et **ya da** önce [cancel-pending-orders-without-saga.sql](cancel-pending-orders-without-saga.sql) çalıştır |
| Saga instance'ı var ama `ExpiresAt` boş (yalnızca Step 3 ayrı deploy edilirse) | Zamanlanmış timeout yok. `OrderTimeoutSweeper` bunları `CreatedAt + PaymentTimeout + SweeperGrace` geçince saga üzerinden iptal ettirir | Yine de Step 3 ile 4'ü birlikte deploy et |

## 5. Açık kalan riskler

- **Kaçan timeout → `OrderTimeoutSweeper`.** Delayed mesaj kaybolursa (RabbitMQ node kaybı) sweeper her
  `OrderSaga:SweeperInterval` (varsayılan 1 dk) aralıkta `AwaitingStock/AwaitingPayment` durumundaki ve
  `ExpiresAt + OrderSaga:SweeperGrace` (varsayılan 2 dk) süresini geçmiş instance'lara `PaymentTimeoutExpired` gönderir.
  Order'a yazmaz, kararı saga verir. Kalan sınırlar:
  - Tur başına 100 instance işlenir. Saga yetişemezse aynı instance'lar sonraki turda tekrar gönderilir (mükerrer gönderim güvenli).
  - Order'ın birden fazla instance'ı çalışıyorsa her biri tarama yapar. Bu yalnızca mükerrer mesaj üretir, tutarsızlık üretmez.
  - Mesaj outbox'tan değil doğrudan bus'tan gönderilir. Kaybolursa instance bir sonraki turda yeniden seçilir.
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
