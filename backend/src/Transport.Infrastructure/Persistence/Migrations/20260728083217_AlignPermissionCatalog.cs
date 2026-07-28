using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Transport.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000003"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                columns: new[] { "code", "description" },
                values: new object[] { "users.read", "Kullanıcıları görüntüler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"),
                columns: new[] { "code", "description" },
                values: new object[] { "users.manage", "Kullanıcı ve rol atamalarını yönetir." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"),
                columns: new[] { "code", "description" },
                values: new object[] { "roles.read", "Rolleri ve yetkileri görüntüler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                columns: new[] { "code", "description" },
                values: new object[] { "roles.manage", "Rolleri ve yetkileri yönetir." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"),
                columns: new[] { "code", "description" },
                values: new object[] { "stops.read", "Durakları görüntüler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"),
                columns: new[] { "code", "description" },
                values: new object[] { "stops.create", "Durak oluşturur." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"),
                columns: new[] { "code", "description" },
                values: new object[] { "stops.update", "Durak günceller." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"),
                columns: new[] { "code", "description" },
                values: new object[] { "stops.delete", "Durak siler veya arşivler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"),
                columns: new[] { "code", "description" },
                values: new object[] { "transit_lines.read", "Güzergâhları görüntüler." });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "description" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000010"), "transit_lines.create", "Güzergâh oluşturur." },
                    { new Guid("40000000-0000-0000-0000-000000000011"), "transit_lines.update", "Güzergâh günceller." },
                    { new Guid("40000000-0000-0000-0000-000000000012"), "transit_lines.delete", "Güzergâh siler veya arşivler." },
                    { new Guid("40000000-0000-0000-0000-000000000013"), "transit_lines.reorder_stops", "Güzergâh durak sırasını değiştirir." },
                    { new Guid("40000000-0000-0000-0000-000000000014"), "route_paths.read", "Üretilmiş rotaları görüntüler." },
                    { new Guid("40000000-0000-0000-0000-000000000015"), "route_paths.generate", "Gerçek yol geometrisi üretir." },
                    { new Guid("40000000-0000-0000-0000-000000000016"), "route_paths.delete", "Üretilmiş rotayı siler veya arşivler." },
                    { new Guid("40000000-0000-0000-0000-000000000017"), "transport.publish", "Ulaşım verisini yayımlar." },
                    { new Guid("40000000-0000-0000-0000-000000000018"), "audit.read", "Audit kayıtlarını görüntüler." }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000008"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000009"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("40000000-0000-0000-0000-000000000009"), new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("40000000-0000-0000-0000-000000000010"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000011"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000012"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000013"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000014"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000015"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000016"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000017"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000018"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000010"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000011"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000012"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000013"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000014"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000015"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000016"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000014"), new Guid("33333333-3333-3333-3333-333333333333") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000010"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000011"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000012"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000013"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000014"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000015"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000016"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000017"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000018"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000008"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000009"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000010"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000011"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000012"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000013"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000014"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000015"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000016"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000005"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000009"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("40000000-0000-0000-0000-000000000014"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000018"));

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                columns: new[] { "code", "description" },
                values: new object[] { "users.manage", "Kullanıcı ve rol atamalarını yönetir." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"),
                columns: new[] { "code", "description" },
                values: new object[] { "stops.read", "Durakları görüntüler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"),
                columns: new[] { "code", "description" },
                values: new object[] { "stops.write", "Durakları oluşturur ve düzenler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                columns: new[] { "code", "description" },
                values: new object[] { "transit-lines.read", "Güzergâhları görüntüler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"),
                columns: new[] { "code", "description" },
                values: new object[] { "transit-lines.write", "Güzergâhları ve durak sırasını yönetir." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"),
                columns: new[] { "code", "description" },
                values: new object[] { "routes.read", "Üretilmiş rotaları görüntüler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"),
                columns: new[] { "code", "description" },
                values: new object[] { "routes.generate", "Gerçek yol geometrisi üretir." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"),
                columns: new[] { "code", "description" },
                values: new object[] { "routes.publish", "Rotaları yayımlar veya arşivler." });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"),
                columns: new[] { "code", "description" },
                values: new object[] { "audit.read", "Audit kayıtlarını görüntüler." });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("33333333-3333-3333-3333-333333333333") }
                });
        }
    }
}
