using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Transport.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "route_paths",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transit_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    color_override = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    geometry = table.Column<LineString>(type: "geography(LineString,4326)", nullable: true),
                    distance_meters = table.Column<double>(type: "double precision", nullable: false),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: false),
                    routing_engine = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    failure_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_paths", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "route_path_stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_path_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<Point>(type: "geography(Point,4326)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_path_stops", x => x.id);
                    table.ForeignKey(
                        name: "FK_route_path_stops_route_paths_route_path_id",
                        column: x => x.route_path_id,
                        principalTable: "route_paths",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_route_paths_transit_line_id",
                table: "route_paths",
                column: "transit_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_paths_transit_line_id_version",
                table: "route_paths",
                columns: new[] { "transit_line_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_path_stops_route_path_id_sequence",
                table: "route_path_stops",
                columns: new[] { "route_path_id", "sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "route_path_stops");
            migrationBuilder.DropTable(name: "route_paths");
        }
    }
}
