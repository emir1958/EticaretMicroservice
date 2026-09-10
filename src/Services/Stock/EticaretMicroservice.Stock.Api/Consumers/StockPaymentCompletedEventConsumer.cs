using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class StockPaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IStockService _stockService;
    private readonly ILogger<StockPaymentCompletedEventConsumer> _logger;

    public StockPaymentCompletedEventConsumer(IStockService stockService, ILogger<StockPaymentCompletedEventConsumer> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("[CorrelationId: {CorrelationId}] Stock.API: PaymentCompletedEvent alındı. OrderId: {OrderId}. Rezervasyon kesinleştiriliyor...",
            message.CorrelationId, message.OrderId);
        foreach (var item in message.OrderItems)
        {
            await _stockService.ConfirmReservationAsync(item.ProductId, item.Quantity);
        }
       
    }
}