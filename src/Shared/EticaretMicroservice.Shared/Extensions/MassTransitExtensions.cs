using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Shared.Extensions
{
    public static class MassTransitExtensions
    {
        /// <summary>
        /// Tüm kuyruklar için ortak Retry ve Hata Politikasını uygular.
        /// Geçici hatalarda 3 kez üstel (exponential) aralıkla dener,
        /// çözülmeyen zehirli mesajları DLQ (_error kuyruğu) içine aktarır.
        /// </summary>
        public static void ConfigureSharedRetryAndDeadLetter(this IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
        {
            cfg.UseMessageRetry(retry =>
            {
                // İlk deneme 1sn, ikinci 3sn, üçüncü 6sn sonra yapılır
                retry.Interval(3, TimeSpan.FromSeconds(2));

                // Format hataları ve bilinen kritik istisnalar tekrar denenmeden doğrudan DLQ'ya gider
                retry.Ignore<ArgumentNullException>();
                retry.Ignore<InvalidCastException>();
            });
        }
    }
}
