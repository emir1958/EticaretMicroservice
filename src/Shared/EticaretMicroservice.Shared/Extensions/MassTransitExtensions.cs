using MassTransit;
using System;

namespace EticaretMicroservice.Shared.Extensions;

public static class MassTransitExtensions
{
    public static void ConfigureSharedRetryAndDeadLetter(this IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.UseMessageRetry(retry =>
        {
            retry.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(10),
                intervalDelta: TimeSpan.FromSeconds(2)
            );

            retry.Ignore<ArgumentNullException>();
            retry.Ignore<InvalidCastException>();
            retry.Ignore<FormatException>();
        });

       
        cfg.UseDelayedRedelivery(r =>
        {
            r.Intervals(
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMinutes(1)
            );

            r.Ignore<ArgumentNullException>();
            r.Ignore<InvalidCastException>();
            r.Ignore<FormatException>();
        });
    }
}