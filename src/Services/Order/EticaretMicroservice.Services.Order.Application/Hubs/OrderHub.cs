using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EticaretMicroservice.Services.Order.Application.Hubs;

[Authorize]
public class OrderHub : Hub
{
    // Grup adı istemciden alınmaz, doğrulanmış token'dan okunur: kimse başkasının bildirimlerine abone olamaz
    public async Task JoinOrderGroup()
    {
        if (BuyerId is { Length: > 0 } buyerId)
            await Groups.AddToGroupAsync(Context.ConnectionId, buyerId);
    }

    public async Task LeaveOrderGroup()
    {
        if (BuyerId is { Length: > 0 } buyerId)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, buyerId);
    }

    private string? BuyerId =>
        Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
}
