namespace Transport.Domain.RoutePaths;

public enum RoutePathStatus
{
    Generating = 0,
    Ready = 1,
    Failed = 2,
    OutOfDate = 3,
    Archived = 4,
}
