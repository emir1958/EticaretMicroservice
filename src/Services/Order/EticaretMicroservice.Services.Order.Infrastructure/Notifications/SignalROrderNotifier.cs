using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EticaretMicroservice.Services.Order.Infrastructure.Notifications;

public class SignalROrderNotifier : IOrderNotifier
{
    private readonly IHubContext<OrderHub> _hubContext;

    public SignalROrderNotifier(IHubContext<OrderHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // Mesaj adı ve gövdesi frontend sözleşmesidir (useOrderHub.js); değiştirilmemelidir
    public Task NotifyOrderStateAsync(string buyerId, int orderId, string status, string message, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(buyerId).SendAsync("ReceiveOrderState", new
        {
            OrderId = orderId,
            Status = status,
            Message = message
        }, cancellationToken);
}
