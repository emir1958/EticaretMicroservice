using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.IntegrationTests.Infrastructure;

// Stock ve Payment servislerinin yerini tutar: Order'dan çıkan mesajları sadece alır.
// Outbox mesajları commit sonrası serileştirilmiş haliyle gönderildiği için harness'ın Published listesi
// bunları tanımaz; doğrulama harness.Consumed (aşağı akışa gerçekten ulaşan mesaj) üzerinden yapılır.
public class SagaMessageSink :
    IConsumer<OrderCreatedEvent>,
    IConsumer<RefundPaymentCommand>,
    IConsumer<ReleaseStockCommand>
{
    public Task Consume(ConsumeContext<OrderCreatedEvent> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<RefundPaymentCommand> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<ReleaseStockCommand> context) => Task.CompletedTask;
}
