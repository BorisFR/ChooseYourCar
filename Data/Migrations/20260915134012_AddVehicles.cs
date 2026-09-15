using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChooseYourCar.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Brand = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Generation = table.Column<string>(type: "TEXT", nullable: false),
                    Trim = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    Technology = table.Column<string>(type: "TEXT", nullable: false),
                    PowerHp = table.Column<int>(type: "INTEGER", nullable: true),
                    Transmission = table.Column<string>(type: "TEXT", nullable: false),
                    PriceIncludingTax = table.Column<double>(type: "REAL", nullable: true),
                    PriceExcludingTax = table.Column<double>(type: "REAL", nullable: true),
                    Seats = table.Column<int>(type: "INTEGER", nullable: true),
                    TrunkLiters = table.Column<int>(type: "INTEGER", nullable: true),
                    RangeWltpKm = table.Column<int>(type: "INTEGER", nullable: true),
                    Co2GramsPerKm = table.Column<int>(type: "INTEGER", nullable: true),
                    AverageRating = table.Column<double>(type: "REAL", nullable: false),
                    RatingCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FavoriteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
