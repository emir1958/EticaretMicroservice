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

            // 2. DbContext ChangeTracker'a ekle (Henüz veritabanına commit EDILEMEDİ)
            var savedOrder = await _orderRepository.AddAsync(newOrder);

            // 3. Event Publish Et (Outbox aktif olduğu için bu mesaj doğrudan RabbitMQ'ya değil, 
            //    DbContext'in OutboxMessage tablosuna eklenecektir)
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
                Payment = new PaymentMessage
                {
                    CardName = request.Payment.CardName,
                    CardNumber = request.Payment.CardNumber,
                    Expiration = request.Payment.Expiration,
                    Cvc = request.Payment.Cvc
                }
            };

            await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);

            // 4. 🔥 TEK TRANSACTION: Hem 'Orders' hem de 'OutboxMessage' tablosu 
            //    aynı SaveChangesAsync ile SQL Server'a atomik olarak yazılır!
            await _orderRepository.SaveChangesAsync(cancellationToken);
            // (Not: Repository'nde SaveChangesAsync yoksa _dbContext.SaveChangesAsync() çağrılmalıdır)

            return savedOrder.Id;
        }
    }
}