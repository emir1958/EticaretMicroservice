using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Stock.IntegrationTests.Infrastructure;

// Payment ve Order servislerinin yerini tutar: Stock'un yayınladığı event'leri sadece alır,
// böylece harness.Consumed üzerinden "aşağı akışa kaç event ulaştı" doğrulanabilir.
public class DownstreamEventSink : IConsumer<StockReservedEvent>, IConsumer<StockFailedEvent>
{
    public Task Consume(ConsumeContext<StockReservedEvent> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<StockFailedEvent> context) => Task.CompletedTask;
}
