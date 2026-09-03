using MediatR;
using MassTransit;
using Microsoft.AspNetCore.Http;
using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using EticaretMicroservice.Shared.Events;

namespace EticaretMicroservice.Services.Order.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICatalogRepository _catalogRepository; // 👈 1. Catalog İstemcisi Enjeksiyonu

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint,
        IHttpContextAccessor httpContextAccessor,
        ICatalogRepository catalogRepository)
    {
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
        _httpContextAccessor = httpContextAccessor;
        _catalogRepository = catalogRepository;
    }

    public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. CorrelationId Tespiti
        var correlationIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();

        Guid correlationId = !string.IsNullOrWhiteSpace(correlationIdHeader) && Guid.TryParse(correlationIdHeader, out var parsedId)
            ? parsedId
            : Guid.NewGuid();

        // 2. Adres Value Object
        var address = new Address(
            request.Address.City,
            request.Address.District,
            request.Address.Street,
            request.Address.ZipCode,
            request.Address.Line
        );

        var newOrder = new Domain.Entities.Order(request.BuyerId, address);

        // 🟢 3. SERVER-SIDE FİYAT DOĞRULAMA (İstemci fiyat manipülasyonu engelleniyor)
        foreach (var item in request.OrderItems)
        {
            var catalogProduct = await _catalogRepository.GetProductByIdAsync(item.ProductId, cancellationToken);

            if (catalogProduct == null)
            {
                throw new InvalidOperationException($"Geçersiz ürün: '{item.ProductId}' katalogda bulunamadı.");
            }

            newOrder.AddOrderItem(
                productId: catalogProduct.Id,
                productName: catalogProduct.Name,
                price: catalogProduct.Price, 
                quantity: item.Quantity
            );
        }

        // 4. Sipariş Entity Kaydı (Henüz Commit Değil)
        var savedOrder = await _orderRepository.AddAsync(newOrder);

        // 5. Outbox Event Hazırlığı
        var paymentToken = request.Payment != null && !string.IsNullOrWhiteSpace(request.Payment.PaymentToken)
            ? request.Payment.PaymentToken
            : Guid.NewGuid().ToString();

        var orderCreatedEvent = new OrderCreatedEvent
        {
            CorrelationId = correlationId,
            OrderId = savedOrder.Id,
            BuyerId = savedOrder.BuyerId,
            OrderItems = savedOrder.OrderItems.Select(x => new OrderItemMessage
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity,
                Price = x.Price 
            }).ToList(),
            PaymentToken = paymentToken
        };

        await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);

        await _orderRepository.SaveChangesAsync(cancellationToken);

        return savedOrder.Id;
    }
}