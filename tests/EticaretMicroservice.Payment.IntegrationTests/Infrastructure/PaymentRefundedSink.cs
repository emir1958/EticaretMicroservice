using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Payment.IntegrationTests.Infrastructure;

// Order saga'sının yerini tutar: Payment'tan çıkan iade event'ini yalnızca alır
public class PaymentRefundedSink : IConsumer<PaymentRefundedEvent>
{
    public Task Consume(ConsumeContext<PaymentRefundedEvent> context) => Task.CompletedTask;
}
