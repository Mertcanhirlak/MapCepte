using Transport.Application.TransitLines;
using Transport.Domain.RoutePaths;

namespace Transport.Application.RoutePaths;

public sealed record GenerateRoutePathCommand(
    TransitLineAccessContext Access,
    Guid TransitLineId,
    string Name,
    RoutePathDirection Direction,
    string? ColorOverride);

public sealed record RoutePathCatalogItem(
    Guid Id,
    Guid TransitLineId,
    string Name,
    string Direction,
    int Version,
    string Status,
    string? ColorOverride,
    double DistanceMeters,
    double DurationSeconds,
    string RoutingEngine,
    DateTimeOffset? GeneratedAtUtc,
    string? FailureCode,
    string? FailureMessage,
    int StopCount,
    double[][]? Coordinates);

public sealed record RoutePathResult(
    RoutePathManagementStatus Status,
    RoutePathCatalogItem? RoutePath = null,
    string? Error = null);

public enum RoutePathManagementStatus
{
    Success = 0,
    InvalidInput = 1,
    NotFound = 2,
    Forbidden = 3,
    InsufficientStops = 4,
    GenerationFailed = 5,
}
