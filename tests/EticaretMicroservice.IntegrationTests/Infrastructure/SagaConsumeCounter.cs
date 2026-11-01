using EticaretMicroservice.Services.Order.Infrastructure.Messaging;
using MassTransit;

namespace EticaretMicroservice.IntegrationTests.Infrastructure;

// Saga endpoint'inde hatasız tamamlanan tüketimleri sayar. Retry'da başarısız olan denemeler ConsumeFault'a düşer, sayılmaz.
public class SagaConsumeCounter<T> : IConsumeMessageObserver<T> where T : class
{
    private readonly Func<T, bool> _filter;
    private int _count;

    public SagaConsumeCounter(Func<T, bool> filter) => _filter = filter;

    public int Count => Volatile.Read(ref _count);

    public Task PreConsume(ConsumeContext<T> context) => Task.CompletedTask;

    public Task PostConsume(ConsumeContext<T> context)
    {
        if (context.ReceiveContext.InputAddress.AbsolutePath.EndsWith(OrderMessagingExtensions.SagaEndpointName)
            && _filter(context.Message))
            Interlocked.Increment(ref _count);

        return Task.CompletedTask;
    }

    public Task ConsumeFault(ConsumeContext<T> context, Exception exception) => Task.CompletedTask;
}
