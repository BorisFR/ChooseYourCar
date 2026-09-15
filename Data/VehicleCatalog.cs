namespace ChooseYourCar.Data;

public static class VehicleCatalog
{
    public static IQueryable<Vehicle> ApplyFilters(
        IQueryable<Vehicle> vehicles,
        string? search,
        string? brand,
        string? technology,
        double? maxPrice,
        int? minRange)
    {
        vehicles = vehicles.Where(vehicle => vehicle.Status == VehicleStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            vehicles = vehicles.Where(vehicle =>
                vehicle.Brand.Contains(normalizedSearch) ||
                vehicle.Model.Contains(normalizedSearch) ||
                vehicle.Trim.Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            vehicles = vehicles.Where(vehicle => vehicle.Brand == brand);
        }

        if (!string.IsNullOrWhiteSpace(technology))
        {
            vehicles = vehicles.Where(vehicle => vehicle.Technology == technology);
        }

        if (maxPrice.HasValue)
        {
            vehicles = vehicles.Where(vehicle => vehicle.PriceIncludingTax.HasValue && vehicle.PriceIncludingTax <= maxPrice);
        }

        if (minRange.HasValue)
        {
            vehicles = vehicles.Where(vehicle => (vehicle.RangeWltpKm ?? vehicle.RangeKm).HasValue && (vehicle.RangeWltpKm ?? vehicle.RangeKm) >= minRange);
        }

        return vehicles;
    }

    public static void Seed(ApplicationDbContext db)
    {
        if (db.Vehicles.Any())
        {
            return;
        }

        db.Vehicles.AddRange(
            new Vehicle
            {
                Brand = "Tesla", Model = "Model 3", Generation = "Highland", Trim = "Grande Autonomie", Year = 2026,
                Technology = "Electrique", PowerHp = 498, Transmission = "Automatique", PriceIncludingTax = 50990,
                PriceExcludingTax = 42492, Seats = 5, TrunkLiters = 594, RangeWltpKm = 629, AverageRating = 4.7,
                RatingCount = 34, FavoriteCount = 128, ViewCount = 1560, Status = VehicleStatus.Published,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
            },
            new Vehicle
            {
                Brand = "Tesla", Model = "Model Y", Generation = "Juniper", Trim = "Propulsion", Year = 2026,
                Technology = "Electrique", PowerHp = 255, Transmission = "Automatique", PriceIncludingTax = 44990,
                PriceExcludingTax = 37492, Seats = 5, TrunkLiters = 869, RangeWltpKm = 500, AverageRating = 4.5,
                RatingCount = 51, FavoriteCount = 203, ViewCount = 2401, Status = VehicleStatus.Published,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            },
            new Vehicle
            {
                Brand = "Renault", Model = "5 E-Tech", Generation = "Crossover", Trim = "Techno", Year = 2026,
                Technology = "Electrique", PowerHp = 150, Transmission = "Automatique", PriceIncludingTax = 32990,
                PriceExcludingTax = 27492, Seats = 5, TrunkLiters = 326, RangeWltpKm = 410, AverageRating = 4.3,
                RatingCount = 19, FavoriteCount = 87, ViewCount = 1120, Status = VehicleStatus.Published,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-4)
            },
            new Vehicle
            {
                Brand = "Peugeot", Model = "3008", Generation = "III", Trim = "Allure", Year = 2026,
                Technology = "Hybride", PowerHp = 136, Transmission = "Automatique", PriceIncludingTax = 38990d,
                PriceExcludingTax = 32492, Seats = 5, TrunkLiters = 520, AverageRating = 4.1,
                RatingCount = 12, FavoriteCount = 61, ViewCount = 886, Status = VehicleStatus.Published,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-7)
            },
            new Vehicle
            {
                Brand = "Citroen", Model = "C3", Generation = "IV", Trim = "Plus", Year = 2026,
                Technology = "Essence", PowerHp = 100, Transmission = "Mecanique", PriceIncludingTax = 18900d,
                PriceExcludingTax = 15750, Seats = 5, TrunkLiters = 310, AverageRating = 3.9,
                RatingCount = 8, FavoriteCount = 44, ViewCount = 645, Status = VehicleStatus.Published,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-9)
            },
            new Vehicle
            {
                Brand = "BYD", Model = "Seal U", Generation = "DM-i", Trim = "Design", Year = 2026,
                Technology = "Hybride rechargeable", PowerHp = 324, Transmission = "Automatique", PriceIncludingTax = 48990,
                PriceExcludingTax = 40825, Seats = 5, TrunkLiters = 425, RangeWltpKm = 70, AverageRating = 4.0,
                RatingCount = 6, FavoriteCount = 32, ViewCount = 412, Status = VehicleStatus.Published,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-12)
            },
            new Vehicle
            {
                Brand = "Tesla", Model = "Model S", Generation = "Plaid", Trim = "Plaid", Year = 2026,
                Technology = "Electrique", PowerHp = 1020, Transmission = "Automatique", PriceIncludingTax = 109990,
                PriceExcludingTax = 91658, Seats = 5, RangeWltpKm = 600, AverageRating = 4.8,
                RatingCount = 21, FavoriteCount = 98, ViewCount = 734, Status = VehicleStatus.Draft,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-3)
            });

        db.SaveChanges();
    }
}
