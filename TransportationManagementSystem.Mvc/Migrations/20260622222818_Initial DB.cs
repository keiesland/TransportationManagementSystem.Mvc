using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportationManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TripDates",
                columns: table => new
                {
                    TripDateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripDates", x => x.TripDateId);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    DriverId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TripId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.DriverId);
                });

            migrationBuilder.CreateTable(
                name: "Summaries",
                columns: table => new
                {
                    SummaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    TripDateId = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<TimeSpan>(type: "time", nullable: false),
                    Out1 = table.Column<TimeSpan>(type: "time", nullable: false),
                    In1 = table.Column<TimeSpan>(type: "time", nullable: false),
                    Out2 = table.Column<TimeSpan>(type: "time", nullable: false),
                    In2 = table.Column<TimeSpan>(type: "time", nullable: false),
                    Out3 = table.Column<TimeSpan>(type: "time", nullable: false),
                    In3 = table.Column<TimeSpan>(type: "time", nullable: false),
                    Out4 = table.Column<TimeSpan>(type: "time", nullable: false),
                    In4 = table.Column<TimeSpan>(type: "time", nullable: false),
                    End = table.Column<TimeSpan>(type: "time", nullable: false),
                    ActualTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    WeeklyTime = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Summaries", x => x.SummaryId);
                    table.ForeignKey(
                        name: "FK_Summaries_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "DriverId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Summaries_TripDates_TripDateId",
                        column: x => x.TripDateId,
                        principalTable: "TripDates",
                        principalColumn: "TripDateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    TripId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    TripDateId = table.Column<int>(type: "int", nullable: false),
                    TripActualStart = table.Column<TimeSpan>(type: "time", nullable: false),
                    ScheduledPickup = table.Column<TimeSpan>(type: "time", nullable: false),
                    PickupArrival = table.Column<TimeSpan>(type: "time", nullable: false),
                    ActualPickup = table.Column<TimeSpan>(type: "time", nullable: false),
                    ActualDropoff = table.Column<TimeSpan>(type: "time", nullable: false),
                    ScheduledDropoff = table.Column<TimeSpan>(type: "time", nullable: false),
                    TripActualEnd = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.TripId);
                    table.ForeignKey(
                        name: "FK_Trips_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "DriverId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trips_TripDates_TripDateId",
                        column: x => x.TripDateId,
                        principalTable: "TripDates",
                        principalColumn: "TripDateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_TripId",
                table: "Drivers",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Summaries_DriverId_TripDateId_Start_Out1",
                table: "Summaries",
                columns: new[] { "DriverId", "TripDateId", "Start", "Out1" });

            migrationBuilder.CreateIndex(
                name: "IX_Summaries_TripDateId",
                table: "Summaries",
                column: "TripDateId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DriverId_TripDateId_TripActualStart_ScheduledPickup",
                table: "Trips",
                columns: new[] { "DriverId", "TripDateId", "TripActualStart", "ScheduledPickup" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TripDateId",
                table: "Trips",
                column: "TripDateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Trips_TripId",
                table: "Drivers",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Trips_TripId",
                table: "Drivers");

            migrationBuilder.DropTable(
                name: "Summaries");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "TripDates");
        }
    }
}
