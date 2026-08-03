using NetTopologySuite.Geometries;
using Transport.Application.Stops;
using Transport.Application.TransitLines;
using Transport.Domain.Stops;
using Transport.Domain.TransitLines;

namespace Transport.UnitTests;

public sealed class TransitLineManagementServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatesDraftTransitLine()
    {
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var service = CreateService(lineRepository, stopRepository);
        var operatorId = Guid.NewGuid();

        var result = await service.CreateAsync(
            new CreateTransitLineCommand(
                new TransitLineAccessContext(operatorId, IsAdmin: false, IsOperator: true),
                " 100 Merkez Hat ",
                " m-100 ",
                " Açıklama ",
                "#ff0000"));

        Assert.Equal(TransitLineManagementStatus.Success, result.Status);
        Assert.NotNull(result.TransitLine);
        var entity = Assert.IsType<TransitLine>(lineRepository.AddedTransitLine);
        Assert.Equal("100 Merkez Hat", entity.Name);
        Assert.Equal("M-100", entity.NormalizedCode);
        Assert.Equal("#FF0000", entity.Color);
        Assert.Equal(TransitLineStatus.Draft, entity.Status);
        Assert.Equal(operatorId, entity.OwnerUserId);
        Assert.Equal(1, lineRepository.SaveCount);
    }

    [Theory]
    [InlineData("", "M1", "#FF0000")]
    [InlineData("Hat 1", "", "#FF0000")]
    [InlineData("Hat 1", "M1", "invalid-color")]
    public async Task RejectsInvalidInput(string name, string code, string color)
    {
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var service = CreateService(lineRepository, stopRepository);

        var result = await service.CreateAsync(
            new CreateTransitLineCommand(
                new TransitLineAccessContext(Guid.NewGuid(), IsAdmin: false, IsOperator: true),
                name,
                code,
                null,
                color));

        Assert.Equal(TransitLineManagementStatus.InvalidInput, result.Status);
        Assert.Null(lineRepository.AddedTransitLine);
    }

    [Fact]
    public async Task RejectsDuplicateCode()
    {
        var lineRepository = new FakeTransitLineRepository();
        lineRepository.ExistingCodes.Add("M-100");
        var stopRepository = new FakeStopRepository();
        var service = CreateService(lineRepository, stopRepository);

        var result = await service.CreateAsync(
            new CreateTransitLineCommand(
                new TransitLineAccessContext(Guid.NewGuid(), IsAdmin: false, IsOperator: true),
                "Merkez Hat",
                "m-100",
                null,
                "#00FF00"));

        Assert.Equal(TransitLineManagementStatus.DuplicateCode, result.Status);
        Assert.Null(lineRepository.AddedTransitLine);
    }

    [Fact]
    public async Task UsesOwnershipAndPublishedVisibilityScopes()
    {
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var service = CreateService(lineRepository, stopRepository);
        var userId = Guid.NewGuid();

        await service.ListAsync(
            new TransitLineListQuery(
                new TransitLineAccessContext(userId, IsAdmin: false, IsOperator: true),
                null,
                1,
                20));
        Assert.Equal(TransitLineVisibilityScope.Owned, lineRepository.LastQuery?.Scope);

        await service.ListAsync(
            new TransitLineListQuery(
                new TransitLineAccessContext(userId, IsAdmin: false, IsOperator: false),
                null,
                1,
                20));
        Assert.Equal(TransitLineVisibilityScope.Published, lineRepository.LastQuery?.Scope);

        await service.ListAsync(
            new TransitLineListQuery(
                new TransitLineAccessContext(userId, IsAdmin: true, IsOperator: false),
                null,
                1,
                20));
        Assert.Equal(TransitLineVisibilityScope.All, lineRepository.LastQuery?.Scope);
    }

    [Fact]
    public async Task UpdatesTransitLineWhenOperatorOwnsIt()
    {
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var operatorId = Guid.NewGuid();
        var existingLine = new TransitLine(
            Guid.NewGuid(),
            "Eski Hat",
            "OLD-1",
            null,
            "#000000",
            ownerUserId: operatorId,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        lineRepository.Items[existingLine.Id] = existingLine;
        var service = CreateService(lineRepository, stopRepository);

        var result = await service.UpdateAsync(
            new UpdateTransitLineCommand(
                new TransitLineAccessContext(operatorId, IsAdmin: false, IsOperator: true),
                existingLine.Id,
                "Yeni Hat",
                "NEW-1",
                "Yeni Açıklama",
                "#123456",
                ExpectedVersion: 1));

        Assert.Equal(TransitLineManagementStatus.Success, result.Status);
        Assert.Equal("Yeni Hat", existingLine.Name);
        Assert.Equal("NEW-1", existingLine.Code);
        Assert.Equal(2, existingLine.Version);
    }

    [Fact]
    public async Task RejectsUpdateWhenOperatorDoesNotOwnIt()
    {
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var ownerId = Guid.NewGuid();
        var otherOperatorId = Guid.NewGuid();
        var existingLine = new TransitLine(
            Guid.NewGuid(),
            "Eski Hat",
            "OLD-1",
            null,
            "#000000",
            ownerUserId: ownerId,
            createdByUserId: ownerId,
            createdAtUtc: FixedNow);

        lineRepository.Items[existingLine.Id] = existingLine;
        var service = CreateService(lineRepository, stopRepository);

        var result = await service.UpdateAsync(
            new UpdateTransitLineCommand(
                new TransitLineAccessContext(otherOperatorId, IsAdmin: false, IsOperator: true),
                existingLine.Id,
                "Yeni Hat",
                "NEW-1",
                null,
                "#123456",
                ExpectedVersion: 1));

        Assert.Equal(TransitLineManagementStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task AddsStopsAndResequencesOnRemovalAndReorders()
    {
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var operatorId = Guid.NewGuid();

        var existingLine = new TransitLine(
            Guid.NewGuid(),
            "Test Hat",
            "TST-1",
            null,
            "#112233",
            ownerUserId: operatorId,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);
        lineRepository.Items[existingLine.Id] = existingLine;

        var stop1 = CreateStop("Durak 1", "D1");
        var stop2 = CreateStop("Durak 2", "D2");
        var stop3 = CreateStop("Durak 3", "D3");
        stopRepository.Stops[stop1.Id] = stop1;
        stopRepository.Stops[stop2.Id] = stop2;
        stopRepository.Stops[stop3.Id] = stop3;

        var service = CreateService(lineRepository, stopRepository);
        var access = new TransitLineAccessContext(operatorId, IsAdmin: false, IsOperator: true);

        // 1. Add Stop 1
        var addRes1 = await service.AddStopAsync(
            new AddStopToLineCommand(access, existingLine.Id, stop1.Id, ExpectedVersion: 1));
        Assert.Equal(TransitLineManagementStatus.Success, addRes1.Status);
        Assert.Single(addRes1.Stops!);
        Assert.Equal(2, existingLine.Version);

        // 2. Add Stop 2
        var addRes2 = await service.AddStopAsync(
            new AddStopToLineCommand(access, existingLine.Id, stop2.Id, ExpectedVersion: 2));
        Assert.Equal(TransitLineManagementStatus.Success, addRes2.Status);
        Assert.Equal(2, addRes2.Stops!.Count);
        Assert.Equal(3, existingLine.Version);

        // 3. Add Stop 3
        var addRes3 = await service.AddStopAsync(
            new AddStopToLineCommand(access, existingLine.Id, stop3.Id, ExpectedVersion: 3));
        Assert.Equal(TransitLineManagementStatus.Success, addRes3.Status);
        Assert.Equal(3, addRes3.Stops!.Count);
        Assert.Equal(4, existingLine.Version);

        // 4. Duplicate addition should be rejected
        var dupRes = await service.AddStopAsync(
            new AddStopToLineCommand(access, existingLine.Id, stop1.Id, ExpectedVersion: 4));
        Assert.Equal(TransitLineManagementStatus.StopAlreadyInLine, dupRes.Status);

        // 5. Reorder stops to [Stop 3, Stop 1, Stop 2]
        var reorderRes = await service.ReorderStopsAsync(
            new ReorderLineStopsCommand(
                access,
                existingLine.Id,
                new[] { stop3.Id, stop1.Id, stop2.Id },
                ExpectedVersion: 4));
        Assert.Equal(TransitLineManagementStatus.Success, reorderRes.Status);
        var orderedStops = reorderRes.Stops!.OrderBy(s => s.Sequence).ToList();
        Assert.Equal(stop3.Id, orderedStops[0].StopId);
        Assert.Equal(1, orderedStops[0].Sequence);
        Assert.Equal(stop1.Id, orderedStops[1].StopId);
        Assert.Equal(2, orderedStops[1].Sequence);
        Assert.Equal(stop2.Id, orderedStops[2].StopId);
        Assert.Equal(3, orderedStops[2].Sequence);
        Assert.Equal(5, existingLine.Version);

        // 6. Remove Stop 1 and check resequencing of remaining stops
        var removeRes = await service.RemoveStopAsync(
            new RemoveStopFromLineCommand(access, existingLine.Id, stop1.Id, ExpectedVersion: 5));
        Assert.Equal(TransitLineManagementStatus.Success, removeRes.Status);
        Assert.Equal(2, removeRes.Stops!.Count);
        var remainingStops = removeRes.Stops!.OrderBy(s => s.Sequence).ToList();
        Assert.Equal(stop3.Id, remainingStops[0].StopId);
        Assert.Equal(1, remainingStops[0].Sequence);
        Assert.Equal(stop2.Id, remainingStops[1].StopId);
        Assert.Equal(2, remainingStops[1].Sequence);
        Assert.Equal(6, existingLine.Version);
    }

    private static Stop CreateStop(string name, string code) =>
        new(
            Guid.NewGuid(),
            name,
            code,
            null,
            "#001122",
            32.8,
            39.9,
            Guid.NewGuid(),
            FixedNow);

    private static TransitLineManagementService CreateService(
        FakeTransitLineRepository lineRepository,
        FakeStopRepository stopRepository)
    {
        return new TransitLineManagementService(
            lineRepository,
            stopRepository,
            new TransitLineAccessPolicy(),
            new FixedTimeProvider(FixedNow));
    }

    private sealed class FakeTransitLineRepository : ITransitLineRepository
    {
        public Dictionary<Guid, TransitLine> Items { get; } = [];
        public HashSet<string> ExistingCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public TransitLine? AddedTransitLine { get; private set; }
        public TransitLineRepositoryQuery? LastQuery { get; private set; }
        public int SaveCount { get; private set; }

        public Task<TransitLineRepositoryPage> ListAsync(
            TransitLineRepositoryQuery query,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            var list = Items.Values.ToList();
            return Task.FromResult(new TransitLineRepositoryPage(list, list.Count));
        }

        public Task<bool> CodeExistsAsync(
            string normalizedCode,
            Guid? excludedTransitLineId,
            CancellationToken cancellationToken)
        {
            if (ExistingCodes.Contains(normalizedCode))
            {
                return Task.FromResult(true);
            }

            var existsInItems = Items.Values.Any(item =>
                item.NormalizedCode == normalizedCode
                && (!excludedTransitLineId.HasValue || item.Id != excludedTransitLineId.Value));

            return Task.FromResult(existsInItems);
        }

        public Task<TransitLine?> FindByIdAsync(
            Guid transitLineId,
            CancellationToken cancellationToken)
        {
            Items.TryGetValue(transitLineId, out var line);
            return Task.FromResult(line);
        }

        public Task AddAsync(TransitLine transitLineEntity, CancellationToken cancellationToken)
        {
            AddedTransitLine = transitLineEntity;
            Items[transitLineEntity.Id] = transitLineEntity;
            return Task.CompletedTask;
        }

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeStopRepository : IStopRepository
    {
        public Dictionary<Guid, Stop> Stops { get; } = [];

        public Task<StopRepositoryPage> ListAsync(StopRepositoryQuery query, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> CodeExistsAsync(string normalizedCode, Guid? excludedStopId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Stop?> FindByIdAsync(Guid stopId, CancellationToken cancellationToken)
        {
            Stops.TryGetValue(stopId, out var stop);
            return Task.FromResult(stop);
        }

        public Task<IReadOnlyDictionary<Guid, Stop>> FindByIdsAsync(IEnumerable<Guid> stopIds, CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, Stop> dict = stopIds
                .Where(id => Stops.ContainsKey(id))
                .ToDictionary(id => id, id => Stops[id]);
            return Task.FromResult(dict);
        }

        public Task AddAsync(Stop stopEntity, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
