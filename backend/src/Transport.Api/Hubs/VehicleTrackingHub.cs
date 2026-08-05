using Microsoft.AspNetCore.SignalR;
using Transport.Application.Vehicles;

namespace Transport.Api.Hubs;

public sealed class VehicleTrackingHub : Hub
{
    public async Task SubscribeToLine(string transitLineId)
    {
        if (!string.IsNullOrWhiteSpace(transitLineId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Line_{transitLineId}");
        }
    }

    public async Task UnsubscribeFromLine(string transitLineId)
    {
        if (!string.IsNullOrWhiteSpace(transitLineId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Line_{transitLineId}");
        }
    }
}
