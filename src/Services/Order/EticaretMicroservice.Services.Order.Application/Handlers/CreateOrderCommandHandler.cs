using MediatR;
using MassTransit;
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

        public CreateOrderCommandHandler(IOrderRepository orderRepository, IPublishEndpoint publishEndpoint)
        {
            _orderRepository = orderRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
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

            // 2. DbContext ChangeTracker'a ekle (Veritabanına HENÜZ yazılmadı)
            var savedOrder = await _orderRepository.AddAsync(newOrder);

            // 🟢 DÜZELTME: Buradaki ilk SaveChangesAsync kaldırıldı! 
            // Sipariş ID'si Identity/Sequence ise EF bunu bellekte hazırlar, 
            // Outbox event'i ile birlikte en sonda TEK SaveChangesAsync çağrılır.

            // 3. Event Publish Et (MassTransit bunu DbContext ChangeTracker'daki OutboxMessage tablosuna ekler)
            var orderCreatedEvent = new OrderCreatedEvent
            {
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

            // 4. 🔥 TEK TRANSACTION: Hem 'Orders' hem de 'OutboxMessage' tablosu 
            //    atomik olarak tek SaveChangesAsync ile SQL Server'a yazılır!
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return savedOrder.Id;
        }
    }
}