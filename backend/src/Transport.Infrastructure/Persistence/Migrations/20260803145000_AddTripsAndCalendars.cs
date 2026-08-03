using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transport.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTripsAndCalendars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operating_calendars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    days_of_week = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operating_calendars", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transit_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_path_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operating_calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    departure_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trip_stop_times",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    arrival_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    departure_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_stop_times", x => x.id);
                    table.ForeignKey(
                        name: "FK_trip_stop_times_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trips_operating_calendar_id",
                table: "trips",
                column: "operating_calendar_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_transit_line_id",
                table: "trips",
                column: "transit_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_trip_stop_times_stop_id",
                table: "trip_stop_times",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "IX_trip_stop_times_trip_id_sequence",
                table: "trip_stop_times",
                columns: new[] { "trip_id", "sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "trip_stop_times");
            migrationBuilder.DropTable(name: "trips");
            migrationBuilder.DropTable(name: "operating_calendars");
        }
    }
}
