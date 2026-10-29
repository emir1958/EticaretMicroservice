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
    private readonly ICatalogRepository _catalogRepository;

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
        // 1. CorrelationId Tespiti (Önce Command, yoksa HTTP Header, yoksa Yeni Guid)
        Guid correlationId = request.CorrelationId;
        if (correlationId == Guid.Empty)
        {
            var correlationIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
            correlationId = !string.IsNullOrWhiteSpace(correlationIdHeader) && Guid.TryParse(correlationIdHeader, out var parsedId)
                ? parsedId
                : Guid.NewGuid();
        }

        // 2. Adres Value Object
        var address = new Address(
            request.Address.City,
            request.Address.District,
            request.Address.Street,
            request.Address.ZipCode,
            request.Address.Line
        );

        var newOrder = new Domain.Entities.Order(request.BuyerId, address);

        // 3. Katalog Üzerinden Ürün ve Fiyat Doğrulama (İstemci fiyatı asla kullanılmaz)
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

        // ... (1, 2 ve 3. Katalog Doğrulama adımları aynı kalır) ...

        // 🟢 4, 5 ve 6. Adımlar: Transaction Scope içine alınır. 
        // Ya Sipariş ve Event aynı anda veritabanına yazılır, ya da ikisi birden iptal olur.
        return await _orderRepository.ExecuteInTransactionAsync(async () =>
        {
            // İlk kayıt (Sadece transaction içinde IDENTITY ID'yi üretmek için)
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

            // Blok başarıyla bittiğinde Transaction Commitlenir ve tüm veriler kalıcı hale gelir
            return savedOrder.Id;

        }, cancellationToken);
    }
}