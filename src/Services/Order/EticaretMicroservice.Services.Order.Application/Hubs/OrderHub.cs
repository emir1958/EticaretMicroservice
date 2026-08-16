using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace EticaretMicroservice.Services.Order.Application.Hubs;

public class OrderHub : Hub
{
    // Frontend (React/Vue vb.) bağlantı kurduğunda client'ı kendi userId'sine özel gruba ekleyebiliriz
    public async Task JoinOrderGroup(string buyerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, buyerId);
    }
}