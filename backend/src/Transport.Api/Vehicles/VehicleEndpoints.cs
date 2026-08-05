using Microsoft.AspNetCore.SignalR;
using Transport.Api.Hubs;
using Transport.Application.Vehicles;

namespace Transport.Api.Vehicles;

public static class VehicleEndpoints
{
    public static IEndpointRouteBuilder MapVehicleEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/vehicles")
            .WithTags("Vehicles");

        group.MapPost(
            "/telemetry",
            async (
                IngestVehiclePositionCommand command,
                VehicleTrackingService service,
                IHubContext<VehicleTrackingHub> hubContext,
                CancellationToken cancellationToken) =>
            {
                var catalogItem = await service.IngestPositionAsync(command, cancellationToken);

                // Broadcast real-time telemetry to subscribed clients via SignalR
                await hubContext.Clients
                    .Group($"Line_{catalogItem.TransitLineId}")
                    .SendAsync("ReceiveVehiclePosition", catalogItem, cancellationToken);

                await hubContext.Clients
                    .All
                    .SendAsync("ReceiveVehiclePosition", catalogItem, cancellationToken);

                return Results.Ok(catalogItem);
            });

        endpoints.MapGet(
            "/api/transit-lines/{transitLineId:guid}/vehicles",
            async (
                Guid transitLineId,
                VehicleTrackingService service,
                CancellationToken cancellationToken) =>
            {
                var positions = await service.GetLatestPositionsByLineAsync(transitLineId, cancellationToken);
                return Results.Ok(positions);
            })
            .WithTags("Vehicles");

        return endpoints;
    }
}
