namespace Transport.Domain.Identity;

public static class PermissionNames
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string StopsRead = "stops.read";
    public const string StopsCreate = "stops.create";
    public const string StopsUpdate = "stops.update";
    public const string StopsDelete = "stops.delete";
    public const string TransitLinesRead = "transit_lines.read";
    public const string TransitLinesCreate = "transit_lines.create";
    public const string TransitLinesUpdate = "transit_lines.update";
    public const string TransitLinesDelete = "transit_lines.delete";
    public const string TransitLinesReorderStops = "transit_lines.reorder_stops";
    public const string RoutePathsRead = "route_paths.read";
    public const string RoutePathsGenerate = "route_paths.generate";
    public const string RoutePathsDelete = "route_paths.delete";
    public const string TransportPublish = "transport.publish";
    public const string AuditRead = "audit.read";

    public static IReadOnlyCollection<string> All { get; } =
    [
        UsersRead,
        UsersManage,
        RolesRead,
        RolesManage,
        StopsRead,
        StopsCreate,
        StopsUpdate,
        StopsDelete,
        TransitLinesRead,
        TransitLinesCreate,
        TransitLinesUpdate,
        TransitLinesDelete,
        TransitLinesReorderStops,
        RoutePathsRead,
        RoutePathsGenerate,
        RoutePathsDelete,
        TransportPublish,
        AuditRead,
    ];
}
