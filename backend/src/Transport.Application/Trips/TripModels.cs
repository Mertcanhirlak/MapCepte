using Transport.Application.TransitLines;
using Transport.Domain.RoutePaths;

namespace Transport.Application.Trips;

public sealed record CreateTripCommand(
    TransitLineAccessContext Access,
    Guid TransitLineId,
    Guid RoutePathId,
    Guid OperatingCalendarId,
    string TripCode,
    TimeOnly DepartureTime,
    RoutePathDirection Direction);

public sealed record ShiftTripTimeCommand(
    TransitLineAccessContext Access,
    Guid TripId,
    int MinutesOffset);

public sealed record TripStopTimeDto(
    Guid Id,
    Guid StopId,
    int Sequence,
    TimeOnly ArrivalTime,
    TimeOnly DepartureTime);

public sealed record TripCatalogItem(
    Guid Id,
    Guid TransitLineId,
    Guid RoutePathId,
    Guid OperatingCalendarId,
    string TripCode,
    TimeOnly DepartureTime,
    string Direction,
    bool IsPublished,
    IReadOnlyCollection<TripStopTimeDto> StopTimes);

public sealed record TimetableStopHeaderDto(
    Guid StopId,
    int Sequence);

public sealed record TimetableMatrixDto(
    Guid TransitLineId,
    IReadOnlyCollection<TimetableStopHeaderDto> Stops,
    IReadOnlyCollection<TripCatalogItem> Trips);

public sealed record TripResult(
    TripManagementStatus Status,
    TripCatalogItem? Trip = null,
    string? Error = null);

public enum TripManagementStatus
{
    Success = 0,
    InvalidInput = 1,
    NotFound = 2,
    Forbidden = 3,
}
