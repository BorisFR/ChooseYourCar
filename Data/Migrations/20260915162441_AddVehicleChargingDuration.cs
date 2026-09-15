using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChooseYourCar.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleChargingDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChargingEndPercent",
                table: "Vehicles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChargingStartPercent",
                table: "Vehicles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChargingTimeMinutes",
                table: "Vehicles",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargingEndPercent",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ChargingStartPercent",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ChargingTimeMinutes",
                table: "Vehicles");
        }
    }
}
