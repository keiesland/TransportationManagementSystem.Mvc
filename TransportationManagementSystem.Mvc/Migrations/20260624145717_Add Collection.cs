using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportationManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Trips_TripId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_TripId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Drivers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "Drivers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_TripId",
                table: "Drivers",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Trips_TripId",
                table: "Drivers",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "TripId");
        }
    }
}
