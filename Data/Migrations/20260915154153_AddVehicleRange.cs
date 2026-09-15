using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChooseYourCar.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RangeKm",
                table: "Vehicles",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RangeKm",
                table: "Vehicles");
        }
    }
}
