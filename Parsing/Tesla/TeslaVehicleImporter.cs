using ChooseYourCar.Data;
using Microsoft.EntityFrameworkCore;

namespace ChooseYourCar.Parsing.Tesla;

public sealed class TeslaVehicleImporter(
    ApplicationDbContext db,
    HttpClient httpClient,
    IWebHostEnvironment environment,
    ILogger<TeslaVehicleImporter> logger)
{
    public async Task<TeslaImportResult> ImportAsync(
        TeslaParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        var imported = 0;
        var created = 0;
        var imageFailures = 0;

        foreach (var snapshot in parseResult.Vehicles)
        {
            var vehicle = await db.Vehicles.SingleOrDefaultAsync(
                item => item.Brand == snapshot.Brand && item.Model == snapshot.Model,
                cancellationToken);

            if (vehicle is null)
            {
                vehicle = new Vehicle
                {
                    Brand = snapshot.Brand,
                    Model = snapshot.Model,
                    Status = VehicleStatus.Draft
                };
                db.Vehicles.Add(vehicle);
                created++;
            }

            var imageDownloaded = await ApplySnapshotAsync(vehicle, snapshot, parseResult.Source, parseResult.CollectedAtUtc, cancellationToken);
            if (!imageDownloaded && !string.IsNullOrWhiteSpace(snapshot.ImageUrl))
            {
                imageFailures++;
            }
            imported++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new TeslaImportResult(imported, created, imageFailures);
    }

    private async Task<bool> ApplySnapshotAsync(
        Vehicle vehicle,
        TeslaVehicleSnapshot snapshot,
        Uri source,
        DateTime collectedAtUtc,
        CancellationToken cancellationToken)
    {
        vehicle.Trim = snapshot.Trim ?? vehicle.Trim;
        vehicle.PriceIncludingTax = snapshot.PriceIncludingTax ?? vehicle.PriceIncludingTax;
        vehicle.PriceExcludingTax = snapshot.PriceExcludingTax ?? vehicle.PriceExcludingTax;
        vehicle.Technology = snapshot.Technology ?? vehicle.Technology;
        vehicle.PowerHp = snapshot.PowerHp ?? vehicle.PowerHp;
        vehicle.Transmission = snapshot.Transmission ?? vehicle.Transmission;
        vehicle.Seats = snapshot.Seats ?? vehicle.Seats;
        vehicle.TrunkLiters = snapshot.TrunkLiters ?? vehicle.TrunkLiters;
        vehicle.RangeKm = snapshot.RangeKm ?? vehicle.RangeKm;
        vehicle.RangeWltpKm = snapshot.RangeWltpKm ?? vehicle.RangeWltpKm;
        vehicle.Co2GramsPerKm = snapshot.Co2GramsPerKm ?? vehicle.Co2GramsPerKm;
        vehicle.BatteryCapacityKwh = snapshot.BatteryCapacityKwh ?? vehicle.BatteryCapacityKwh;
        vehicle.AccelerationSeconds = snapshot.AccelerationSeconds ?? vehicle.AccelerationSeconds;
        vehicle.ChargingStartPercent = snapshot.ChargingStartPercent ?? vehicle.ChargingStartPercent;
        vehicle.ChargingEndPercent = snapshot.ChargingEndPercent ?? vehicle.ChargingEndPercent;
        vehicle.ChargingTimeMinutes = snapshot.ChargingTimeMinutes ?? vehicle.ChargingTimeMinutes;
        var localImageUrl = await DownloadImageAsync(snapshot, cancellationToken);
        vehicle.ImageUrl = localImageUrl ?? vehicle.ImageUrl;
        vehicle.ManufacturerUrl = snapshot.ManufacturerUrl ?? vehicle.ManufacturerUrl;
        vehicle.ImportSource = source.ToString();
        vehicle.CollectedAtUtc = collectedAtUtc;
        vehicle.UpdatedAtUtc = collectedAtUtc;
        return localImageUrl is not null || string.IsNullOrWhiteSpace(snapshot.ImageUrl);
    }

    private async Task<string?> DownloadImageAsync(
        TeslaVehicleSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(snapshot.ImageUrl))
        {
            return null;
        }

        var uploadsDirectory = Path.Combine(environment.WebRootPath, "uploads", "vehicles");
        Directory.CreateDirectory(uploadsDirectory);

        var fileName = $"{NormalizeFileName(snapshot.Brand)}-{NormalizeFileName(snapshot.Model)}.jpg";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
        {
            return $"/uploads/vehicles/{fileName}";
        }

        try
        {
            var imageBytes = await httpClient.GetByteArrayAsync(snapshot.ImageUrl, cancellationToken);
            await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Unable to download Tesla image for {Model} from {ImageUrl}", snapshot.Model, snapshot.ImageUrl);
            return null;
        }

        return $"/uploads/vehicles/{fileName}";
    }

    private static string NormalizeFileName(string model) =>
        model.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);
}

public sealed record TeslaImportResult(int ImportedCount, int CreatedCount, int ImageFailureCount);