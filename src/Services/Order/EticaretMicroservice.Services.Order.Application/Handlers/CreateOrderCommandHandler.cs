using MediatR;
using MassTransit;
using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Shared.Exceptions;

namespace EticaretMicroservice.Services.Order.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICatalogRepository _catalogRepository;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint,
        ICatalogRepository catalogRepository)
    {
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
        _catalogRepository = catalogRepository;
    }

    public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Saga anahtarı sunucuda üretilir: istemci header'ı (X-Correlation-ID) yalnızca log/trace içindir.
        // NewId sıralı GUID üretir; OrderState'in clustered PK'sinde rastgele GUID'in yarattığı index parçalanmasını önler.
        var correlationId = NewId.NextGuid();

        var address = new Address(
            request.Address.ContactName,
            request.Address.Phone ?? string.Empty,
            request.Address.City,
            request.Address.District,
            request.Address.Street,
            request.Address.ZipCode ?? string.Empty,
            request.Address.Line
        );

        var newOrder = new Domain.Entities.Order(request.BuyerId, address, correlationId);

        // Ad ve fiyat katalogdan alınır; istemcinin gönderdiği fiyat kullanılmaz
        foreach (var item in request.OrderItems)
        {
            var catalogProduct = await _catalogRepository.GetProductByIdAsync(item.ProductId, cancellationToken);

            if (catalogProduct == null)
            {
                throw new BusinessRuleException($"Geçersiz ürün: '{item.ProductId}' katalogda bulunamadı.");
            }

            newOrder.AddOrderItem(
                productId: catalogProduct.Id,
                productName: catalogProduct.Name,
                price: catalogProduct.Price,
                quantity: item.Quantity
            );
        }

        // Sipariş ve OrderCreatedEvent (outbox) aynı transaction'da yazılır: ya ikisi birden kalıcı olur ya hiçbiri
        return await _orderRepository.ExecuteInTransactionAsync(async () =>
        {
            // Event sipariş numarasını taşıdığı için önce kaydedilip IDENTITY değeri alınır
            var savedOrder = await _orderRepository.AddAsync(newOrder);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            var paymentToken = request.Payment != null && !string.IsNullOrWhiteSpace(request.Payment.PaymentToken)
                ? request.Payment.PaymentToken
                : Guid.NewGuid().ToString();

            var orderCreatedEvent = new OrderCreatedEvent
            {
                CorrelationId = correlationId,
                OrderId = savedOrder.Id,
                BuyerId = savedOrder.BuyerId,
                PaymentToken = paymentToken,
                OrderItems = savedOrder.OrderItems.Select(x => new OrderItemMessage
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Price = x.Price,
                    Quantity = x.Quantity
                }).ToList()
            };

            // Event Outbox'a eklenir ve ikinci SaveChanges çağrılır
            await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return savedOrder.Id;
        }, cancellationToken);
    }
}