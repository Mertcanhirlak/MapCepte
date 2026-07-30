using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using NetTopologySuite.Geometries;

namespace Transport.Domain.Stops;

[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Stop is the agreed transport-domain term.")]
public sealed class Stop
{
    private static readonly SearchValues<char> HexadecimalCharacters =
        SearchValues.Create("0123456789abcdefABCDEF");

    private Stop()
    {
    }

    public Stop(
        Guid id,
        string name,
        string? code,
        string? description,
        string color,
        double longitude,
        double latitude,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Stop id cannot be empty.", nameof(id));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Creator user id cannot be empty.",
                nameof(createdByUserId));
        }

        Id = id;
        Name = RequireText(name, nameof(name));
        Code = NormalizeOptionalText(code);
        NormalizedCode = Code?.ToUpperInvariant();
        Description = NormalizeOptionalText(description);
        Color = RequireColor(color);
        Location = CreateLocation(longitude, latitude);
        Status = StopStatus.Draft;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Code { get; private set; }

    public string? NormalizedCode { get; private set; }

    public string? Description { get; private set; }

    public string Color { get; private set; } = string.Empty;

    public Point Location { get; private set; } = null!;

    public StopStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private static Point CreateLocation(double longitude, double latitude)
    {
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                "Longitude must be between -180 and 180.");
        }

        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                "Latitude must be between -90 and 90.");
        }

        return new Point(longitude, latitude)
        {
            SRID = 4326,
        };
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string RequireColor(string value)
    {
        var color = RequireText(value, nameof(value));
        if (color.Length != 7
            || color[0] != '#'
            || color.AsSpan(1).ContainsAnyExcept(HexadecimalCharacters))
        {
            throw new ArgumentException(
                "Color must use #RRGGBB format.",
                nameof(value));
        }

        return color.ToUpperInvariant();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
