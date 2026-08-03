using System.Buffers;
using NetTopologySuite.Geometries;

namespace Transport.Domain.RoutePaths;

public sealed class RoutePath
{
    private static readonly SearchValues<char> HexadecimalCharacters =
        SearchValues.Create("0123456789abcdefABCDEF");

    private readonly List<RoutePathStop> stops = [];

    private RoutePath()
    {
    }

    public RoutePath(
        Guid id,
        Guid transitLineId,
        string name,
        RoutePathDirection direction,
        int version,
        string? colorOverride,
        string routingEngine,
        string inputHash,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Route path id cannot be empty.", nameof(id));
        }

        if (transitLineId == Guid.Empty)
        {
            throw new ArgumentException("Transit line id cannot be empty.", nameof(transitLineId));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Creator user id cannot be empty.", nameof(createdByUserId));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be greater than zero.");
        }

        Id = id;
        TransitLineId = transitLineId;
        Name = RequireText(name, nameof(name));
        Direction = direction;
        Version = version;
        ColorOverride = NormalizeOptionalColor(colorOverride);
        Status = RoutePathStatus.Generating;
        RoutingEngine = RequireText(routingEngine, nameof(routingEngine));
        InputHash = RequireText(inputHash, nameof(inputHash));
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TransitLineId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public RoutePathDirection Direction { get; private set; }

    public int Version { get; private set; }

    public RoutePathStatus Status { get; private set; }

    public string? ColorOverride { get; private set; }

    public LineString? Geometry { get; private set; }

    public double DistanceMeters { get; private set; }

    public double DurationSeconds { get; private set; }

    public string RoutingEngine { get; private set; } = string.Empty;

    public string InputHash { get; private set; } = string.Empty;

    public DateTimeOffset? GeneratedAtUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public string? FailureCode { get; private set; }

    public string? FailureMessage { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<RoutePathStop> Stops => stops;

    public void CompleteGeneration(
        LineString geometry,
        double distanceMeters,
        double durationSeconds,
        IEnumerable<(Guid stopId, int sequence, double longitude, double latitude)> stopSnapshots,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (geometry.SRID != 4326)
        {
            throw new ArgumentException("Geometry must have SRID 4326.", nameof(geometry));
        }

        if (distanceMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceMeters), "Distance cannot be negative.");
        }

        if (durationSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration cannot be negative.");
        }

        Geometry = geometry;
        DistanceMeters = distanceMeters;
        DurationSeconds = durationSeconds;
        Status = RoutePathStatus.Ready;
        GeneratedAtUtc = generatedAtUtc.ToUniversalTime();
        UpdatedAtUtc = GeneratedAtUtc.Value;
        FailureCode = null;
        FailureMessage = null;

        stops.Clear();
        foreach (var (stopId, sequence, longitude, latitude) in stopSnapshots)
        {
            stops.Add(new RoutePathStop(Guid.NewGuid(), Id, stopId, sequence, longitude, latitude));
        }
    }

    public void FailGeneration(string failureCode, string failureMessage, DateTimeOffset failedAtUtc)
    {
        Status = RoutePathStatus.Failed;
        FailureCode = RequireText(failureCode, nameof(failureCode));
        FailureMessage = RequireText(failureMessage, nameof(failureMessage));
        UpdatedAtUtc = failedAtUtc.ToUniversalTime();
    }

    public void MarkOutOfDate(DateTimeOffset updatedAtUtc)
    {
        if (Status == RoutePathStatus.Ready)
        {
            Status = RoutePathStatus.OutOfDate;
            UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
        }
    }

    public void Archive(DateTimeOffset updatedAtUtc)
    {
        Status = RoutePathStatus.Archived;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length != 7
            || trimmed[0] != '#'
            || trimmed.AsSpan(1).ContainsAnyExcept(HexadecimalCharacters))
        {
            throw new ArgumentException("Color must use #RRGGBB format.", nameof(value));
        }

        return trimmed.ToUpperInvariant();
    }
}
