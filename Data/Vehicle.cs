namespace ChooseYourCar.Data;

public enum VehicleStatus
{
    Draft,
    PendingValidation,
    Published,
    Archived
}

public sealed class Vehicle
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Generation { get; set; } = string.Empty;
    public string Trim { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string Technology { get; set; } = string.Empty;
    public int? PowerHp { get; set; }
    public string Transmission { get; set; } = string.Empty;
    public double? PriceIncludingTax { get; set; }
    public double? PriceExcludingTax { get; set; }
    public int? Seats { get; set; }
    public int? TrunkLiters { get; set; }
    public int? RangeWltpKm { get; set; }
    public int? Co2GramsPerKm { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int FavoriteCount { get; set; }
    public int ViewCount { get; set; }
    public string? ImageUrl { get; set; }
    public string? ManufacturerUrl { get; set; }
    public VehicleStatus Status { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
