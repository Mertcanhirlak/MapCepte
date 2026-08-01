using Transport.Application.Stops;
using Transport.Domain.Stops;

namespace Transport.UnitTests;

public sealed class StopManagementServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 7, 30, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatesDraftStopWithPostgisReadyCoordinates()
    {
        var repository = new FakeStopRepository();
        var service = CreateService(repository);
        var actorUserId = Guid.NewGuid();

        var result = await service.CreateAsync(
            new CreateStopCommand(
                actorUserId,
                " Merkez Meydan ",
                " mrk-001 ",
                " Ana durak ",
                "#13b8a6",
                32.8597,
                39.9334));

        Assert.Equal(StopManagementStatus.Success, result.Status);
        var stopEntity = Assert.IsType<Stop>(repository.AddedStop);
        Assert.Equal("Merkez Meydan", stopEntity.Name);
        Assert.Equal("MRK-001", stopEntity.NormalizedCode);
        Assert.Equal("#13B8A6", stopEntity.Color);
        Assert.Equal(4326, stopEntity.Location.SRID);
        Assert.Equal(32.8597, stopEntity.Location.X);
        Assert.Equal(39.9334, stopEntity.Location.Y);
        Assert.Equal(StopStatus.Draft, stopEntity.Status);
        Assert.Equal(actorUserId, stopEntity.CreatedByUserId);
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData("#12345", 32.0, 39.0)]
    [InlineData("#123456", 181.0, 39.0)]
    [InlineData("#123456", 32.0, 91.0)]
    public async Task RejectsInvalidColorOrCoordinates(
        string color,
        double longitude,
        double latitude)
    {
        var repository = new FakeStopRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            new CreateStopCommand(
                Guid.NewGuid(),
                "Invalid Stop",
                null,
                null,
                color,
                longitude,
                latitude));

        Assert.Equal(StopManagementStatus.InvalidInput, result.Status);
        Assert.Null(repository.AddedStop);
    }

    [Fact]
    public async Task UsesOwnershipAndPublishedVisibilityScopes()
    {
        var repository = new FakeStopRepository();
        var service = CreateService(repository);
        var actorUserId = Guid.NewGuid();

        await service.ListAsync(
            new StopAccessContext(
                actorUserId,
                IsAdmin: false,
                IsOperator: true));
        Assert.Equal(StopVisibilityScope.Owned, repository.LastScope);

        await service.ListAsync(
            new StopAccessContext(
                actorUserId,
                IsAdmin: false,
                IsOperator: false));
        Assert.Equal(StopVisibilityScope.Published, repository.LastScope);

        await service.ListAsync(
            new StopAccessContext(
                actorUserId,
                IsAdmin: true,
                IsOperator: false));
        Assert.Equal(StopVisibilityScope.All, repository.LastScope);
    }

    [Fact]
    public async Task OwnerUpdatesStopAndAdvancesVersion()
    {
        var ownerId = Guid.NewGuid();
        var repository = new FakeStopRepository
        {
            ExistingStop = CreateStop(ownerId),
        };
        var service = CreateService(repository);

        var result = await service.UpdateAsync(
            new UpdateStopCommand(
                new StopAccessContext(ownerId, false, true),
                repository.ExistingStop.Id,
                "Updated Stop",
                "UPD-001",
                "Updated description",
                "#F6B84A",
                33.0,
                40.0,
                ExpectedVersion: 1));

        Assert.Equal(StopManagementStatus.Success, result.Status);
        Assert.Equal(2, result.Stop?.Version);
        Assert.Equal("Updated Stop", repository.ExistingStop.Name);
        Assert.Equal(ownerId, repository.ExistingStop.UpdatedByUserId);
    }

    [Fact]
    public async Task OtherOperatorCannotUpdateStop()
    {
        var repository = new FakeStopRepository
        {
            ExistingStop = CreateStop(Guid.NewGuid()),
        };
        var service = CreateService(repository);

        var result = await service.UpdateAsync(
            new UpdateStopCommand(
                new StopAccessContext(Guid.NewGuid(), false, true),
                repository.ExistingStop.Id,
                "Unauthorized Update",
                null,
                null,
                "#13B8A6",
                32.0,
                39.0,
                ExpectedVersion: 1));

        Assert.Equal(StopManagementStatus.Forbidden, result.Status);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task RejectsStaleVersionAndArchivesCurrentVersion()
    {
        var ownerId = Guid.NewGuid();
        var repository = new FakeStopRepository
        {
            ExistingStop = CreateStop(ownerId),
        };
        var service = CreateService(repository);
        repository.ExistingStop.UpdateDetails(
            "Already Updated",
            null,
            null,
            "#13B8A6",
            32.0,
            39.0,
            ownerId,
            FixedNow);

        var staleResult = await service.ArchiveAsync(
            new ArchiveStopCommand(
                new StopAccessContext(ownerId, false, true),
                repository.ExistingStop.Id,
                ExpectedVersion: 1));
        Assert.Equal(StopManagementStatus.Conflict, staleResult.Status);

        var archiveResult = await service.ArchiveAsync(
            new ArchiveStopCommand(
                new StopAccessContext(ownerId, false, true),
                repository.ExistingStop.Id,
                ExpectedVersion: 2));
        Assert.Equal(StopManagementStatus.Success, archiveResult.Status);
        Assert.Equal("Archived", archiveResult.Stop?.Status);
        Assert.Equal(3, archiveResult.Stop?.Version);
    }

    private static StopManagementService CreateService(
        FakeStopRepository repository)
    {
        return new StopManagementService(
            repository,
            new StopAccessPolicy(),
            new FixedTimeProvider(FixedNow));
    }

    private static Stop CreateStop(Guid ownerId)
    {
        return new Stop(
            Guid.NewGuid(),
            "Existing Stop",
            "EX-001",
            null,
            "#13B8A6",
            32.0,
            39.0,
            ownerId,
            FixedNow);
    }

    private sealed class FakeStopRepository : IStopRepository
    {
        public Stop? AddedStop { get; private set; }

        public Stop? ExistingStop { get; init; }

        public StopVisibilityScope? LastScope { get; private set; }

        public int SaveCount { get; private set; }

        public Task<IReadOnlyCollection<Stop>> ListAsync(
            Guid actorUserId,
            StopVisibilityScope scope,
            CancellationToken cancellationToken)
        {
            LastScope = scope;
            return Task.FromResult<IReadOnlyCollection<Stop>>([]);
        }

        public Task<bool> CodeExistsAsync(
            string normalizedCode,
            Guid? excludedStopId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<Stop?> FindByIdAsync(
            Guid stopId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ExistingStop?.Id == stopId ? ExistingStop : null);
        }

        public Task AddAsync(
            Stop stopEntity,
            CancellationToken cancellationToken)
        {
            AddedStop = stopEntity;
            return Task.CompletedTask;
        }

        public Task<bool> SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(true);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
