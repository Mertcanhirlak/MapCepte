using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Transport.Domain.Identity;
using Transport.Domain.Stops;
using Transport.Infrastructure.Persistence;

namespace Transport.IntegrationTests;

public sealed class IdentityModelTests
{
    [Fact]
    public void IdentityTablesAndCompositeKeysAreConfigured()
    {
        using var context = CreateContext();

        var userType = context.Model.FindEntityType(typeof(User));
        var roleType = context.Model.FindEntityType(typeof(Role));
        var permissionType = context.Model.FindEntityType(typeof(Permission));
        var userRoleType = context.Model.FindEntityType(typeof(UserRole));
        var rolePermissionType =
            context.Model.FindEntityType(typeof(RolePermission));

        Assert.Equal("users", userType?.GetTableName());
        Assert.Equal("roles", roleType?.GetTableName());
        Assert.Equal("permissions", permissionType?.GetTableName());
        Assert.Equal("user_roles", userRoleType?.GetTableName());
        Assert.Equal("role_permissions", rolePermissionType?.GetTableName());
        Assert.Equal(2, userRoleType?.FindPrimaryKey()?.Properties.Count);
        Assert.Equal(2, rolePermissionType?.FindPrimaryKey()?.Properties.Count);
    }

    [Fact]
    public void ModelSeedsRolesPermissionsAndAssignmentsWithoutAUserPassword()
    {
        using var context = CreateContext();

        var designTimeModel = context.GetService<IDesignTimeModel>().Model;

        var roleSeeds = designTimeModel
            .FindEntityType(typeof(Role))!
            .GetSeedData();
        var permissionSeeds = designTimeModel
            .FindEntityType(typeof(Permission))!
            .GetSeedData();
        var rolePermissionSeeds = designTimeModel
            .FindEntityType(typeof(RolePermission))!
            .GetSeedData();
        var userSeeds = designTimeModel
            .FindEntityType(typeof(User))!
            .GetSeedData();

        var roleNames = roleSeeds
            .Select(seed => Assert.IsType<string>(seed[nameof(Role.Name)]))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            SystemRoleNames.All.ToHashSet(StringComparer.Ordinal),
            roleNames);
        Assert.Equal(PermissionNames.All.Count, permissionSeeds.Count());
        Assert.Equal(33, rolePermissionSeeds.Count());
        Assert.Empty(userSeeds);
    }

    [Fact]
    public void StopUsesGeographyPointAndSpatialIndex()
    {
        using var context = CreateContext();

        var stopType = context.Model.FindEntityType(typeof(Stop));
        var location = stopType?.FindProperty(nameof(Stop.Location));
        var spatialIndex = stopType?.GetIndexes().Single(index =>
            index.GetDatabaseName() == "ix_stops_location");

        Assert.Equal("stops", stopType?.GetTableName());
        Assert.Equal("geography (point, 4326)", location?.GetColumnType());
        Assert.Equal(
            "gist",
            spatialIndex?.FindAnnotation("Npgsql:IndexMethod")?.Value);
    }

    private static TransportDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransportDbContext>()
            .UseNpgsql(
                "Host=127.0.0.1;Database=model-only;Username=model-only",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new TransportDbContext(options);
    }
}
