using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChooseYourCar.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleImportMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CollectedAtUtc",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportSource",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectedAtUtc",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ImportSource",
                table: "Vehicles");
        }
    }
}
