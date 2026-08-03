using System.Buffers;

namespace Transport.Domain.TransitLines;

public sealed class TransitLine
{
    private static readonly SearchValues<char> HexadecimalCharacters =
        SearchValues.Create("0123456789abcdefABCDEF");

    private readonly List<TransitLineStop> stops = [];

    private TransitLine()
    {
    }

    public TransitLine(
        Guid id,
        string name,
        string code,
        string? description,
        string color,
        Guid ownerUserId,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Transit line id cannot be empty.",
                nameof(id));
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner user id cannot be empty.",
                nameof(ownerUserId));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Creator user id cannot be empty.",
                nameof(createdByUserId));
        }

        Id = id;
        Name = RequireText(name, nameof(name));
        Code = RequireText(code, nameof(code));
        NormalizedCode = Code.ToUpperInvariant();
        Description = NormalizeOptionalText(description);
        Color = RequireColor(color);
        Status = TransitLineStatus.Draft;
        OwnerUserId = ownerUserId;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string NormalizedCode { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string Color { get; private set; } = string.Empty;

    public TransitLineStatus Status { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public IReadOnlyCollection<TransitLineStop> Stops => stops;

    public bool IsOwnedBy(Guid userId)
    {
        return userId != Guid.Empty && OwnerUserId == userId;
    }

    public void UpdateDetails(
        string name,
        string code,
        string? description,
        string color,
        Guid updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        EnsureCanChange(updatedByUserId);
        Name = RequireText(name, nameof(name));
        Code = RequireText(code, nameof(code));
        NormalizedCode = Code.ToUpperInvariant();
        Description = NormalizeOptionalText(description);
        Color = RequireColor(color);
        Touch(updatedByUserId, updatedAtUtc);
    }

    public void AddStop(
        Guid transitLineStopId,
        Guid stopId,
        Guid updatedByUserId,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanChange(updatedByUserId);
        if (stops.Exists(item => item.StopId == stopId))
        {
            throw new InvalidOperationException(
                "A stop can only be added to a transit line once.");
        }

        stops.Add(
            new TransitLineStop(
                transitLineStopId,
                Id,
                stopId,
                stops.Count + 1,
                changedAtUtc));
        Touch(updatedByUserId, changedAtUtc);
    }

    public void RemoveStop(
        Guid stopId,
        Guid updatedByUserId,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanChange(updatedByUserId);
        var lineStop = stops.Find(item => item.StopId == stopId)
            ?? throw new InvalidOperationException(
                "The stop does not belong to this transit line.");

        stops.Remove(lineStop);
        ResequenceStops();
        Touch(updatedByUserId, changedAtUtc);
    }

    public void ReorderStops(
        IReadOnlyList<Guid> orderedStopIds,
        Guid updatedByUserId,
        DateTimeOffset changedAtUtc)
    {
        EnsureCanChange(updatedByUserId);
        ArgumentNullException.ThrowIfNull(orderedStopIds);

        if (orderedStopIds.Count != stops.Count
            || orderedStopIds.Distinct().Count() != stops.Count
            || orderedStopIds.Any(stopId =>
                stops.TrueForAll(item => item.StopId != stopId)))
        {
            throw new ArgumentException(
                "Reorder input must contain every transit line stop exactly once.",
                nameof(orderedStopIds));
        }

        var sequenceByStopId = orderedStopIds
            .Select((stopId, index) => new { stopId, sequence = index + 1 })
            .ToDictionary(item => item.stopId, item => item.sequence);

        foreach (var lineStop in stops)
        {
            lineStop.MoveTo(sequenceByStopId[lineStop.StopId]);
        }

        stops.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
        Touch(updatedByUserId, changedAtUtc);
    }

    public void Archive(
        Guid updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        EnsureCanChange(updatedByUserId);
        Status = TransitLineStatus.Archived;
        Touch(updatedByUserId, updatedAtUtc);
    }

    public void Publish(
        Guid updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        EnsureCanChange(updatedByUserId);
        Status = TransitLineStatus.Published;
        Touch(updatedByUserId, updatedAtUtc);
    }

    public void Unpublish(
        Guid updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        EnsureCanChange(updatedByUserId);
        Status = TransitLineStatus.Draft;
        Touch(updatedByUserId, updatedAtUtc);
    }

    private void ResequenceStops()
    {
        stops.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
        for (var index = 0; index < stops.Count; index++)
        {
            stops[index].MoveTo(index + 1);
        }
    }

    private void EnsureCanChange(Guid updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Updater user id cannot be empty.",
                nameof(updatedByUserId));
        }

        if (Status == TransitLineStatus.Archived)
        {
            throw new InvalidOperationException(
                "An archived transit line cannot be changed.");
        }
    }

    private void Touch(Guid updatedByUserId, DateTimeOffset updatedAtUtc)
    {
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
        Version++;
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
