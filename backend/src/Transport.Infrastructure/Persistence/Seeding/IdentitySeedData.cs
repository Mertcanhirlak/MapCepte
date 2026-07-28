using Microsoft.EntityFrameworkCore;
using Transport.Domain.Identity;

namespace Transport.Infrastructure.Persistence.Seeding;

internal static class IdentitySeedData
{
    private static readonly Guid AdminRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid OperatorRoleId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid UserRoleId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Dictionary<string, Guid> PermissionIds =
        new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            [PermissionNames.UsersRead] =
                Guid.Parse("40000000-0000-0000-0000-000000000001"),
            [PermissionNames.UsersManage] =
                Guid.Parse("40000000-0000-0000-0000-000000000002"),
            [PermissionNames.RolesRead] =
                Guid.Parse("40000000-0000-0000-0000-000000000003"),
            [PermissionNames.RolesManage] =
                Guid.Parse("40000000-0000-0000-0000-000000000004"),
            [PermissionNames.StopsRead] =
                Guid.Parse("40000000-0000-0000-0000-000000000005"),
            [PermissionNames.StopsCreate] =
                Guid.Parse("40000000-0000-0000-0000-000000000006"),
            [PermissionNames.StopsUpdate] =
                Guid.Parse("40000000-0000-0000-0000-000000000007"),
            [PermissionNames.StopsDelete] =
                Guid.Parse("40000000-0000-0000-0000-000000000008"),
            [PermissionNames.TransitLinesRead] =
                Guid.Parse("40000000-0000-0000-0000-000000000009"),
            [PermissionNames.TransitLinesCreate] =
                Guid.Parse("40000000-0000-0000-0000-000000000010"),
            [PermissionNames.TransitLinesUpdate] =
                Guid.Parse("40000000-0000-0000-0000-000000000011"),
            [PermissionNames.TransitLinesDelete] =
                Guid.Parse("40000000-0000-0000-0000-000000000012"),
            [PermissionNames.TransitLinesReorderStops] =
                Guid.Parse("40000000-0000-0000-0000-000000000013"),
            [PermissionNames.RoutePathsRead] =
                Guid.Parse("40000000-0000-0000-0000-000000000014"),
            [PermissionNames.RoutePathsGenerate] =
                Guid.Parse("40000000-0000-0000-0000-000000000015"),
            [PermissionNames.RoutePathsDelete] =
                Guid.Parse("40000000-0000-0000-0000-000000000016"),
            [PermissionNames.TransportPublish] =
                Guid.Parse("40000000-0000-0000-0000-000000000017"),
            [PermissionNames.AuditRead] =
                Guid.Parse("40000000-0000-0000-0000-000000000018"),
        };

    public static void Apply(ModelBuilder modelBuilder)
    {
        SeedRoles(modelBuilder);
        SeedPermissions(modelBuilder);
        SeedRolePermissions(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new
            {
                Id = AdminRoleId,
                Name = SystemRoleNames.Admin,
                NormalizedName = SystemRoleNames.Admin.ToUpperInvariant(),
                Description = "Sistem ve kullanıcı yönetimi dahil tüm yetkiler.",
                IsSystem = true,
            },
            new
            {
                Id = OperatorRoleId,
                Name = SystemRoleNames.Operator,
                NormalizedName = SystemRoleNames.Operator.ToUpperInvariant(),
                Description = "Durak, güzergâh ve rota yönetimi yetkileri.",
                IsSystem = true,
            },
            new
            {
                Id = UserRoleId,
                Name = SystemRoleNames.User,
                NormalizedName = SystemRoleNames.User.ToUpperInvariant(),
                Description = "Yayımlanmış ulaşım verilerini görüntüleme yetkileri.",
                IsSystem = true,
            });
    }

    private static void SeedPermissions(ModelBuilder modelBuilder)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PermissionNames.UsersRead] = "Kullanıcıları görüntüler.",
            [PermissionNames.UsersManage] = "Kullanıcı ve rol atamalarını yönetir.",
            [PermissionNames.RolesRead] = "Rolleri ve yetkileri görüntüler.",
            [PermissionNames.RolesManage] = "Rolleri ve yetkileri yönetir.",
            [PermissionNames.StopsRead] = "Durakları görüntüler.",
            [PermissionNames.StopsCreate] = "Durak oluşturur.",
            [PermissionNames.StopsUpdate] = "Durak günceller.",
            [PermissionNames.StopsDelete] = "Durak siler veya arşivler.",
            [PermissionNames.TransitLinesRead] = "Güzergâhları görüntüler.",
            [PermissionNames.TransitLinesCreate] = "Güzergâh oluşturur.",
            [PermissionNames.TransitLinesUpdate] = "Güzergâh günceller.",
            [PermissionNames.TransitLinesDelete] = "Güzergâh siler veya arşivler.",
            [PermissionNames.TransitLinesReorderStops] = "Güzergâh durak sırasını değiştirir.",
            [PermissionNames.RoutePathsRead] = "Üretilmiş rotaları görüntüler.",
            [PermissionNames.RoutePathsGenerate] = "Gerçek yol geometrisi üretir.",
            [PermissionNames.RoutePathsDelete] = "Üretilmiş rotayı siler veya arşivler.",
            [PermissionNames.TransportPublish] = "Ulaşım verisini yayımlar.",
            [PermissionNames.AuditRead] = "Audit kayıtlarını görüntüler.",
        };

        modelBuilder.Entity<Permission>().HasData(
            PermissionNames.All.Select(code => new
            {
                Id = PermissionIds[code],
                Code = code,
                Description = descriptions[code],
            }));
    }

    private static void SeedRolePermissions(ModelBuilder modelBuilder)
    {
        var operatorPermissions = new[]
        {
            PermissionNames.StopsRead,
            PermissionNames.StopsCreate,
            PermissionNames.StopsUpdate,
            PermissionNames.StopsDelete,
            PermissionNames.TransitLinesRead,
            PermissionNames.TransitLinesCreate,
            PermissionNames.TransitLinesUpdate,
            PermissionNames.TransitLinesDelete,
            PermissionNames.TransitLinesReorderStops,
            PermissionNames.RoutePathsRead,
            PermissionNames.RoutePathsGenerate,
            PermissionNames.RoutePathsDelete,
        };

        var userPermissions = new[]
        {
            PermissionNames.StopsRead,
            PermissionNames.TransitLinesRead,
            PermissionNames.RoutePathsRead,
        };

        var assignments = PermissionNames.All
            .Select(code => new
            {
                RoleId = AdminRoleId,
                PermissionId = PermissionIds[code],
            })
            .Concat(operatorPermissions.Select(code => new
            {
                RoleId = OperatorRoleId,
                PermissionId = PermissionIds[code],
            }))
            .Concat(userPermissions.Select(code => new
            {
                RoleId = UserRoleId,
                PermissionId = PermissionIds[code],
            }));

        modelBuilder.Entity<RolePermission>().HasData(assignments);
    }
}
