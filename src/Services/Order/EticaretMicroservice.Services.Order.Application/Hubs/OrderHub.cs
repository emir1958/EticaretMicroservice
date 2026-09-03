using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EticaretMicroservice.Services.Order.Application.Hubs;

[Authorize]
public class OrderHub : Hub
{
    // İstemciden buyerId parametresi ALMIYORUZ; doğrudan doğrulanmış token'dan okuyoruz
    public async Task JoinOrderGroup()
    {
        var buyerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(buyerId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, buyerId);
        }
    }

    public async Task LeaveOrderGroup()
    {
        var buyerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(buyerId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, buyerId);
        }
    }
}