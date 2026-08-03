using Transport.Domain.TransitLines;

namespace Transport.UnitTests;

public sealed class TransitLineDomainTests
{
    [Fact]
    public void NewTransitLineNormalizesValuesAndStartsAsOwnedDraft()
    {
        var ownerUserId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026,
            8,
            1,
            12,
            0,
            0,
            TimeSpan.FromHours(3));

        var line = new TransitLine(
            Guid.NewGuid(),
            "  Kampüs Hattı  ",
            "  kampus-01  ",
            "  Ring güzergâhı  ",
            "#13b8a6",
            ownerUserId,
            ownerUserId,
            createdAt);

        Assert.Equal("Kampüs Hattı", line.Name);
        Assert.Equal("kampus-01", line.Code);
        Assert.Equal("KAMPUS-01", line.NormalizedCode);
        Assert.Equal("Ring güzergâhı", line.Description);
        Assert.Equal("#13B8A6", line.Color);
        Assert.Equal(TransitLineStatus.Draft, line.Status);
        Assert.True(line.IsOwnedBy(ownerUserId));
        Assert.Equal(1, line.Version);
        Assert.Equal(TimeSpan.Zero, line.CreatedAtUtc.Offset);
    }

    [Fact]
    public void StopsCanBeAddedRemovedAndReorderedWithoutGaps()
    {
        var ownerUserId = Guid.NewGuid();
        var line = CreateLine(ownerUserId);
        var firstStopId = Guid.NewGuid();
        var secondStopId = Guid.NewGuid();
        var thirdStopId = Guid.NewGuid();

        line.AddStop(
            Guid.NewGuid(),
            firstStopId,
            ownerUserId,
            DateTimeOffset.UtcNow);
        line.AddStop(
            Guid.NewGuid(),
            secondStopId,
            ownerUserId,
            DateTimeOffset.UtcNow);
        line.AddStop(
            Guid.NewGuid(),
            thirdStopId,
            ownerUserId,
            DateTimeOffset.UtcNow);

        line.ReorderStops(
            [thirdStopId, firstStopId, secondStopId],
            ownerUserId,
            DateTimeOffset.UtcNow);
        Assert.Equal(
            [thirdStopId, firstStopId, secondStopId],
            line.Stops.OrderBy(item => item.Sequence)
                .Select(item => item.StopId));

        line.RemoveStop(
            firstStopId,
            ownerUserId,
            DateTimeOffset.UtcNow);
        Assert.Equal(
            [1, 2],
            line.Stops.OrderBy(item => item.Sequence)
                .Select(item => item.Sequence));
        Assert.Equal(6, line.Version);
    }

    [Fact]
    public void SameStopCannotRepeatWithinLineButCanBelongToAnotherLine()
    {
        var ownerUserId = Guid.NewGuid();
        var stopId = Guid.NewGuid();
        var firstLine = CreateLine(ownerUserId);
        var secondLine = CreateLine(ownerUserId);

        firstLine.AddStop(
            Guid.NewGuid(),
            stopId,
            ownerUserId,
            DateTimeOffset.UtcNow);
        secondLine.AddStop(
            Guid.NewGuid(),
            stopId,
            ownerUserId,
            DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => firstLine.AddStop(
            Guid.NewGuid(),
            stopId,
            ownerUserId,
            DateTimeOffset.UtcNow));
        Assert.Single(firstLine.Stops);
        Assert.Single(secondLine.Stops);
    }

    [Fact]
    public void ArchivedTransitLineRejectsFurtherChanges()
    {
        var ownerUserId = Guid.NewGuid();
        var line = CreateLine(ownerUserId);

        line.Archive(ownerUserId, DateTimeOffset.UtcNow);

        Assert.Equal(TransitLineStatus.Archived, line.Status);
        Assert.Throws<InvalidOperationException>(() => line.AddStop(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ownerUserId,
            DateTimeOffset.UtcNow));
    }

    private static TransitLine CreateLine(Guid ownerUserId)
    {
        return new TransitLine(
            Guid.NewGuid(),
            "Kampüs Hattı",
            $"KMP-{Guid.NewGuid():N}",
            null,
            "#13B8A6",
            ownerUserId,
            ownerUserId,
            DateTimeOffset.UtcNow);
    }
}
