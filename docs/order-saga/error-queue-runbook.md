# Runbook: `_error` kuyruğundaki mesajlar

Bir mesaj tüm denemelerden sonra hâlâ işlenemiyorsa MassTransit onu `<kuyruk>_error` kuyruğuna taşır
(örn. `order-saga_error`). Bu mesajlar kendiliğinden yeniden işlenmez. Biri inceleyip karar vermelidir.

## 1. Mesaj buraya nasıl gelir?

Paylaşılan ayar (`ConfigureSharedRetryAndDeadLetter`) ile:

```
hata → 3 anlık retry (~1-9 sn) → 15 sn bekle → 3 anlık retry → 1 dk bekle → 3 anlık retry → _error
```

Yani bir mesaj `_error`'a düştüğünde yaklaşık 2-2,5 dk boyunca 9 kez denenmiş demektir
(2026-11-08'de gerçek RabbitMQ'da ölçüldü: ilk yeniden deneme ~1 sn, `_error` ~135 sn). Kısa süreli
sorunlar (saga instance'ından önce gelen event, anlık DB kilidi) bu sürede kendiliğinden çözülür.
`_error`'daki mesaj **kalıcı** bir sorunu gösterir.

## 2. İnceleme

RabbitMQ Management UI: <http://localhost:15672> → Queues → `order-saga_error` → **Get messages**.
**Ack mode: "Nack message requeue true"** seçilmelidir. Bu mod mesajı yalnızca gösterir, kuyruktan silmez.

Bakılacak yerler:

| Nerede | Ne söyler |
|---|---|
| Header `MT-Fault-ExceptionType` | Hatanın türü (örn. `MassTransit.SagaException`) |
| Header `MT-Fault-Message` | Hata mesajı. `Saga exception on receipt of ...` = saga instance'ı yok |
| Header `MT-Fault-StackTrace` | Activity içinden fırlayan hatanın yeri |
| Body `messageType` | Event/komut türü (`StockReservedEvent`, `PaymentCompletedEvent`, ...) |
| Body `message.correlationId` | Sipariş süreci. Aşağıdaki SQL ile siparişi bulun |

```sql
-- correlationId ile sipariş ve saga durumu
SELECT o.Id, o.OrderStatus, o.CreatedDate, s.CurrentState, s.ExpiresAt
FROM OrderDb.ordering.Orders o
LEFT JOIN OrderDb.ordering.OrderStates s ON s.CorrelationId = o.CorrelationId
WHERE o.CorrelationId = '<correlationId>';
```

## 3. Karar tablosu (`order-saga_error`)

| Durum | Sebep | Ne yapılır |
|---|---|---|
| Sipariş yok | Sahte/bozuk mesaj veya başka ortamdan gelmiş | **Sil** |
| Sipariş var, saga yok, sipariş saga öncesinden kalma | Step 3 öncesi oluşmuş eski sipariş | Sipariş beklemede ise [cancel-pending-orders-without-saga.sql](cancel-pending-orders-without-saga.sql) ile ele al, sonra mesajı **sil**. Mesaj `PaymentCompletedEvent` ise para çekilmiştir: script bu siparişi "ödemesi alınmış" olarak listeler, tamamlama veya iade kararı elle verilir |
| Sipariş var, saga artık var | Geçici durum (örn. OrderCreated o sırada işlenemedi, DB erişilemedi) | Kök sebep giderildiyse **yeniden kuyruğa al** |
| Activity hatası (`MT-Fault-StackTrace` `CancelOrderActivity` / `CompleteOrderActivity` gösteriyor) | Kod veya veri sorunu | Önce düzelt, sonra **yeniden kuyruğa al** |

Saga'daki tüm geçişler idempotenttir: terminal durumdaki saga kopya event'leri yoksayar. Bu yüzden emin
olunamayan durumda yeniden kuyruğa almak, silmekten daha güvenlidir. En kötü ihtimalle mesaj tekrar `_error`'a düşer.

## 4. Yeniden kuyruğa alma (tek mesaj)

Management UI'daki "Move messages" özelliği shovel plugin'ini gerektirir ve bizim image'ımızda yoktur.
Bunun yerine management API kullanılır. Sıra önemlidir: önce **kopyalanır**, sonra **silinir**.
Böylece arada bir hata olursa mesaj kaybolmaz, en fazla kopyalanmış olur.

```bash
API=http://localhost:15672/api; AUTH=guest:guest   # .env'deki RABBITMQ_USER/PASS

# 1) Baştaki mesajı silmeden oku; message_id ve correlationId'yi kontrol et
curl -s -u $AUTH -H 'content-type: application/json' -X POST "$API/queues/%2F/order-saga_error/get" \
  -d '{"count":1,"ackmode":"reject_requeue_true","encoding":"auto"}'

# 2) Yalnızca body'yi (MassTransit envelope'u) asıl kuyruğa yeniden yayınla.
#    MT-Redelivery-Count / MT-Fault-* header'ları KOPYALANMAZ: aksi halde redelivery hakkı tükenmiş sayılır.
curl -s -u $AUTH -H 'content-type: application/json' -X POST "$API/exchanges/%2F/amq.default/publish" \
  -d '{"properties":{"content_type":"application/vnd.masstransit+json","message_id":"<message_id>"},
       "routing_key":"order-saga","payload":"<1. adımdaki payload, JSON escape edilmiş>","payload_encoding":"string"}'

# 3) Yayın {"routed":true} döndüyse mesajı _error'dan sil (önce 1. adımla aynı mesaj olduğunu doğrula)
curl -s -u $AUTH -H 'content-type: application/json' -X POST "$API/queues/%2F/order-saga_error/get" \
  -d '{"count":1,"ackmode":"ack_requeue_false","encoding":"auto"}'
```

Notlar (2026-11-08'de bu prosedür bir test mesajıyla uygulanarak doğrulandı):
- `_error`'daki kopyada `MT-Redelivery-Count: 2` bulunur, yani iki redelivery aralığı da kullanılmıştır.
  Bu header taşınırsa mesaj redelivery yapılmadan yeniden `_error`'a gider.
- Inbox, başarıyla işlenmiş bir `message_id`'yi kopya sayar. `_error`'daki mesaj hiç başarıyla işlenmediği için
  aynı id ile yeniden yayınlandığında normal şekilde işlenir.

## 5. Silme

Bölüm 4'teki 1. adımla mesajı doğrula, ardından 3. adımı uygula. Kuyruğun tamamını temizlemek (Purge)
yalnızca içindeki **tüm** mesajlar tek tek incelenip "sil" kararı verildiyse yapılmalıdır.

## 6. Sonrası

- Yeniden işlenen siparişin durumunu bölüm 2'deki SQL ile kontrol et.
- Aynı türde hata tekrar ediyorsa kök sebep kodda veya veridedir; mesajları tek tek taşımak yerine onu düzelt.
- `_error` kuyruklarının dolduğunu fark etmek için alarm henüz yok (roadmap Faz 4.1).
