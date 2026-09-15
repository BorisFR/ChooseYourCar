using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChooseYourCar.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleBatteryAndAcceleration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AccelerationSeconds",
                table: "Vehicles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BatteryCapacityKwh",
                table: "Vehicles",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccelerationSeconds",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "BatteryCapacityKwh",
                table: "Vehicles");
        }
    }
}
