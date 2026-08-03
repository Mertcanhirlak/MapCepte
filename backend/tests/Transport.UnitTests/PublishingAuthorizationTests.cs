using Transport.Application.TransitLines;
using Transport.Domain.TransitLines;

namespace Transport.UnitTests;

public sealed class PublishingAuthorizationTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 3, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UserRoleCannotAccessDraftTransitLines()
    {
        var repository = new FakeTransitLineRepository();
        var operatorId = Guid.NewGuid();
        var normalUserId = Guid.NewGuid();

        var draftLine = new TransitLine(
            Guid.NewGuid(),
            "Draft Line",
            "DRAFT-01",
            null,
            "#112233",
            ownerUserId: operatorId,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        repository.Items[draftLine.Id] = draftLine;

        var service = new TransitLineManagementService(
            repository,
            new FakeStopRepository(),
            new TransitLineAccessPolicy(),
            new FixedTimeProvider(FixedNow));

        var userAccess = new TransitLineAccessContext(normalUserId, IsAdmin: false, IsOperator: false);

        var listResult = await service.ListAsync(new TransitLineListQuery(userAccess, Search: null, Page: 1, PageSize: 20));

        Assert.Equal(TransitLineManagementStatus.Success, listResult.Status);
        Assert.Empty(listResult.Page!.Items); // User role sees 0 items because draft is hidden!
    }

    [Fact]
    public async Task OperatorCanPublishOwnTransitLine()
    {
        var repository = new FakeTransitLineRepository();
        var operatorId = Guid.NewGuid();

        var draftLine = new TransitLine(
            Guid.NewGuid(),
            "My Draft Line",
            "M-100",
            null,
            "#123456",
            ownerUserId: operatorId,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        repository.Items[draftLine.Id] = draftLine;

        var service = new TransitLineManagementService(
            repository,
            new FakeStopRepository(),
            new TransitLineAccessPolicy(),
            new FixedTimeProvider(FixedNow));

        var operatorAccess = new TransitLineAccessContext(operatorId, IsAdmin: false, IsOperator: true);

        var publishResult = await service.PublishAsync(operatorAccess, draftLine.Id);

        Assert.Equal(TransitLineManagementStatus.Success, publishResult.Status);
        Assert.NotNull(publishResult.TransitLine);
        Assert.Equal("Published", publishResult.TransitLine.Status);

        // Now normal user can list the published line!
        var userAccess = new TransitLineAccessContext(Guid.NewGuid(), IsAdmin: false, IsOperator: false);
        var userListResult = await service.ListAsync(new TransitLineListQuery(userAccess, Search: null, Page: 1, PageSize: 20));

        Assert.Single(userListResult.Page!.Items);
        Assert.Equal("Published", userListResult.Page!.Items.First().Status);
    }

    [Fact]
    public async Task UnauthorisedOperatorCannotPublishAnotherOperatorsLine()
    {
        var repository = new FakeTransitLineRepository();
        var ownerOperatorId = Guid.NewGuid();
        var attackerOperatorId = Guid.NewGuid();

        var draftLine = new TransitLine(
            Guid.NewGuid(),
            "Protected Line",
            "PROT-01",
            null,
            "#001122",
            ownerUserId: ownerOperatorId,
            createdByUserId: ownerOperatorId,
            createdAtUtc: FixedNow);

        repository.Items[draftLine.Id] = draftLine;

        var service = new TransitLineManagementService(
            repository,
            new FakeStopRepository(),
            new TransitLineAccessPolicy(),
            new FixedTimeProvider(FixedNow));

        var attackerAccess = new TransitLineAccessContext(attackerOperatorId, IsAdmin: false, IsOperator: true);

        var publishResult = await service.PublishAsync(attackerAccess, draftLine.Id);

        Assert.Equal(TransitLineManagementStatus.Forbidden, publishResult.Status);
        Assert.Equal("Draft", draftLine.Status.ToString());
    }

    private sealed class FakeTransitLineRepository : ITransitLineRepository
    {
        public Dictionary<Guid, TransitLine> Items { get; } = [];

        public Task<TransitLineRepositoryPage> ListAsync(TransitLineRepositoryQuery query, CancellationToken cancellationToken)
        {
            var queryable = Items.Values.AsQueryable();

            if (query.Scope == TransitLineVisibilityScope.Published)
            {
                queryable = queryable.Where(l => l.Status == TransitLineStatus.Published);
            }
            else if (query.Scope == TransitLineVisibilityScope.Owned)
            {
                queryable = queryable.Where(l => l.OwnerUserId == query.ActorUserId);
            }

            var list = queryable.ToList();
            return Task.FromResult(new TransitLineRepositoryPage(list, list.Count));
        }

        public Task<bool> CodeExistsAsync(string normalizedCode, Guid? excludedTransitLineId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<TransitLine?> FindByIdAsync(Guid transitLineId, CancellationToken cancellationToken)
        {
            Items.TryGetValue(transitLineId, out var line);
            return Task.FromResult(line);
        }

        public Task AddAsync(TransitLine transitLineEntity, CancellationToken cancellationToken)
        {
            Items[transitLineEntity.Id] = transitLineEntity;
            return Task.CompletedTask;
        }

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeStopRepository : Transport.Application.Stops.IStopRepository
    {
        public Task<Domain.Stops.Stop?> FindByIdAsync(Guid stopId, CancellationToken cancellationToken) => Task.FromResult<Domain.Stops.Stop?>(null);
        public Task<IReadOnlyDictionary<Guid, Domain.Stops.Stop>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<Guid, Domain.Stops.Stop>>(new Dictionary<Guid, Domain.Stops.Stop>());
        public Task AddAsync(Domain.Stops.Stop stopEntity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<Application.Stops.StopRepositoryPage> ListAsync(Application.Stops.StopRepositoryQuery query, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> CodeExistsAsync(string normalizedCode, Guid? excludedStopId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
