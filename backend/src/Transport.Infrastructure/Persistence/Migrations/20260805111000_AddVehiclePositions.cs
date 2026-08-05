using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Transport.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiclePositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transit_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_path_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    speed_kmh = table.Column<double>(type: "double precision", nullable: true),
                    heading = table.Column<double>(type: "double precision", nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_positions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_positions_recorded_at_utc",
                table: "vehicle_positions",
                column: "recorded_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_positions_transit_line_id",
                table: "vehicle_positions",
                column: "transit_line_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "vehicle_positions");
        }
    }
}
