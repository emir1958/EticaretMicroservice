using MediatR;
using MassTransit;
using Microsoft.AspNetCore.Http;
using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using EticaretMicroservice.Shared.Events;

namespace EticaretMicroservice.Services.Order.Application.Handlers
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IHttpContextAccessor _httpContextAccessor; // 🟢 1. HttpContextAccessor Enjeksiyonu

        public CreateOrderCommandHandler(
            IOrderRepository orderRepository,
            IPublishEndpoint publishEndpoint,
            IHttpContextAccessor httpContextAccessor)
        {
            _orderRepository = orderRepository;
            _publishEndpoint = publishEndpoint;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            // 🟢 2. CorrelationId Belirleme (Header'da varsa al, yoksa yeni oluştur)
            var correlationIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();

            Guid correlationId = !string.IsNullOrWhiteSpace(correlationIdHeader) && Guid.TryParse(correlationIdHeader, out var parsedId)
                ? parsedId
                : Guid.NewGuid();

            // 1. Value Object ve Aggregate Root Oluşturma
            var address = new Address(
                request.Address.City,
                request.Address.District,
                request.Address.Street,
                request.Address.ZipCode,
                request.Address.Line
            );

            var newOrder = new Domain.Entities.Order(request.BuyerId, address);

            foreach (var item in request.OrderItems)
            {
                newOrder.AddOrderItem(item.ProductId, item.ProductName, item.Price, item.Quantity);
            }

            // 2. DbContext ChangeTracker'a ekle ve ID'nin (Identity) oluşması için veritabanına yaz
            var savedOrder = await _orderRepository.AddAsync(newOrder);
            await _orderRepository.SaveChangesAsync(cancellationToken); // 🟢 OrderId'nin 0 olmaması için DB commit

            // 3. Event Publish Et (MassTransit Outbox'a ekler)
            var orderCreatedEvent = new OrderCreatedEvent
            {
                CorrelationId = correlationId, // 🟢 3. CorrelationId Akışa Dahil Edildi
                OrderId = savedOrder.Id,
                BuyerId = savedOrder.BuyerId,
                OrderItems = savedOrder.OrderItems.Select(x => new OrderItemMessage
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    Price = x.Price
                }).ToList(),
                PaymentToken = Guid.NewGuid().ToString()
            };

            await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);

            // 4. Outbox mesajını veritabanına kaydet
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return savedOrder.Id;
        }
    }
}