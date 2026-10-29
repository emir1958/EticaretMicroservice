using EticaretMicroservice.Stock.Api.Consumers;
using EticaretMicroservice.Stock.Api.Data;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Extensions;

public static class StockMessagingExtensions
{
    public static IBusRegistrationConfigurator AddStockMessaging(this IBusRegistrationConfigurator x)
    {
        x.AddConsumer<OrderCreatedEventConsumer>();
        x.AddConsumer<PaymentFailedEventConsumer>();
        x.AddConsumer<ProductCreatedEventConsumer>();
        x.AddConsumer<StockUpdatedEventConsumer>();
        x.AddConsumer<StockPaymentCompletedEventConsumer>();

        x.AddEntityFrameworkOutbox<StockDbContext>(o =>
        {
            o.UseSqlServer();
            o.UseBusOutbox(); 
        });

        x.AddConfigureEndpointsCallback((context, _, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<StockDbContext>(context);
        });

        x.SetKebabCaseEndpointNameFormatter();

        return x;
    }
}
