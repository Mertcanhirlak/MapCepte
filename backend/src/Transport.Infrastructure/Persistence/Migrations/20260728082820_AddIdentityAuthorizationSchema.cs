using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Transport.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAuthorizationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "description" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), "users.manage", "Kullanıcı ve rol atamalarını yönetir." },
                    { new Guid("40000000-0000-0000-0000-000000000002"), "stops.read", "Durakları görüntüler." },
                    { new Guid("40000000-0000-0000-0000-000000000003"), "stops.write", "Durakları oluşturur ve düzenler." },
                    { new Guid("40000000-0000-0000-0000-000000000004"), "transit-lines.read", "Güzergâhları görüntüler." },
                    { new Guid("40000000-0000-0000-0000-000000000005"), "transit-lines.write", "Güzergâhları ve durak sırasını yönetir." },
                    { new Guid("40000000-0000-0000-0000-000000000006"), "routes.read", "Üretilmiş rotaları görüntüler." },
                    { new Guid("40000000-0000-0000-0000-000000000007"), "routes.generate", "Gerçek yol geometrisi üretir." },
                    { new Guid("40000000-0000-0000-0000-000000000008"), "routes.publish", "Rotaları yayımlar veya arşivler." },
                    { new Guid("40000000-0000-0000-0000-000000000009"), "audit.read", "Audit kayıtlarını görüntüler." }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "description", "is_system", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Sistem ve kullanıcı yönetimi dahil tüm yetkiler.", true, "Admin", "ADMIN" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Durak, güzergâh ve rota yönetimi yetkileri.", true, "Operator", "OPERATOR" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Yayımlanmış ulaşım verilerini görüntüleme yetkileri.", true, "User", "USER" }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000009"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("33333333-3333-3333-3333-333333333333") }
                });

            migrationBuilder.CreateIndex(
                name: "ux_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ux_roles_normalized_name",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
