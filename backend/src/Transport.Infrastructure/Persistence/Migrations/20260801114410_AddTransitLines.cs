using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transport.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransitLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transit_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transit_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_transit_lines_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transit_lines_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transit_lines_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transit_line_stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transit_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    boarding_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    alighting_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transit_line_stops", x => x.id);
                    table.ForeignKey(
                        name: "FK_transit_line_stops_stops_stop_id",
                        column: x => x.stop_id,
                        principalTable: "stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transit_line_stops_transit_lines_transit_line_id",
                        column: x => x.transit_line_id,
                        principalTable: "transit_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transit_line_stops_stop_id",
                table: "transit_line_stops",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ux_transit_line_stops_line_sequence",
                table: "transit_line_stops",
                columns: new[] { "transit_line_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_transit_line_stops_line_stop",
                table: "transit_line_stops",
                columns: new[] { "transit_line_id", "stop_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transit_lines_created_by_user_id",
                table: "transit_lines",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transit_lines_owner_user_id",
                table: "transit_lines",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transit_lines_updated_by_user_id",
                table: "transit_lines",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_transit_lines_normalized_code",
                table: "transit_lines",
                column: "normalized_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transit_line_stops");

            migrationBuilder.DropTable(
                name: "transit_lines");
        }
    }
}
