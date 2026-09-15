using System.Net;
using System.Text.RegularExpressions;

namespace ChooseYourCar.Parsing.Tesla;

public sealed class TeslaParser(HttpClient httpClient)
{
    private static readonly string[] SupportedModels = ["Model Y", "Model 3", "Model S", "Model X", "Cybertruck"];
    private static readonly Regex ModelRegex = new(@"\b(?:Model\s+(?:Y|3|S|X)|Cybertruck)\b", RegexOptions.CultureInvariant);
    private static readonly Regex PriceRegex = new(@"(?:À|A)\s+partir\s+de\s+([0-9\s\u00A0]+)\s*€", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex LinkRegex = new(@"https?://www\.tesla\.com/fr_fr/(?:model(?<model>y|3|s|x)|cybertruck)(?:/design)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ImageRegex = new(@"https://digitalassets\.tesla\.com/[^""'\s<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public async Task<TeslaParseResult> ParseAsync(Uri source, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(source, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseHtml(source, html, DateTime.UtcNow);
    }

    public static TeslaParseResult ParseHtml(Uri source, string html, DateTime collectedAtUtc)
    {
        var normalizedHtml = WebUtility.HtmlDecode(html);
        var vehicles = SupportedModels
            .Select(model => ParseVehicle(model, source, normalizedHtml, collectedAtUtc))
            .Where(vehicle => vehicle is not null)
            .Cast<TeslaVehicleSnapshot>()
            .ToArray();

        return new TeslaParseResult(
            source,
            collectedAtUtc,
            vehicles,
            BuildCoverageReport(vehicles));
    }

    private static TeslaVehicleSnapshot? ParseVehicle(string model, Uri source, string html, DateTime collectedAtUtc)
    {
        var modelMatch = Regex.Match(html, $@"\b{Regex.Escape(model)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!modelMatch.Success)
        {
            return null;
        }

        var nextModelMatch = ModelRegex.Match(html, modelMatch.Index + modelMatch.Length);
        var contextLength = nextModelMatch.Success ? nextModelMatch.Index - modelMatch.Index : html.Length - modelMatch.Index;
        var context = html.Substring(modelMatch.Index, contextLength);
        var price = PriceRegex.Match(context);
        var link = LinkRegex.Match(context);
        var image = ImageRegex.Match(context);

        return new TeslaVehicleSnapshot
        {
            Brand = "Tesla",
            Model = model,
            ManufacturerUrl = link.Success ? link.Value : new Uri(source, ModelSlug(model)).ToString(),
            PriceIncludingTax = price.Success ? ParsePrice(price.Groups[1].Value) : null,
            ImageUrl = image.Success ? image.Value : null,
            CollectedAtUtc = DateTime.SpecifyKind(collectedAtUtc, DateTimeKind.Utc)
        };
    }

    private static string ModelSlug(string model) => model.Equals("Cybertruck", StringComparison.OrdinalIgnoreCase)
        ? "cybertruck"
        : $"model{model[^1].ToString().ToLowerInvariant()}";

    private static double? ParsePrice(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return double.TryParse(digits, out var price) ? price : null;
    }

    private static IReadOnlyList<TeslaFieldCoverage> BuildCoverageReport(IReadOnlyList<TeslaVehicleSnapshot> vehicles)
    {
        var fields = new (string Name, Func<TeslaVehicleSnapshot, bool> HasValue)[]
        {
            ("identite", vehicle => !string.IsNullOrWhiteSpace(vehicle.Model)),
            ("version", vehicle => !string.IsNullOrWhiteSpace(vehicle.Trim)),
            ("prix_ttc", vehicle => vehicle.PriceIncludingTax.HasValue),
            ("prix_ht", vehicle => vehicle.PriceExcludingTax.HasValue),
            ("motorisation", vehicle => !string.IsNullOrWhiteSpace(vehicle.Technology)),
            ("puissance", vehicle => vehicle.PowerHp.HasValue),
            ("autonomie_wltp", vehicle => vehicle.RangeWltpKm.HasValue),
            ("recharge", vehicle => vehicle.ChargingInformation is not null),
            ("emissions_co2", vehicle => vehicle.Co2GramsPerKm.HasValue),
            ("equipements", vehicle => vehicle.Features.Count > 0),
            ("visuel_principal", vehicle => !string.IsNullOrWhiteSpace(vehicle.ImageUrl)),
            ("url_constructeur", vehicle => !string.IsNullOrWhiteSpace(vehicle.ManufacturerUrl)),
            ("date_collecte", vehicle => vehicle.CollectedAtUtc != default),
            ("date_mise_a_jour", vehicle => vehicle.LastUpdatedAtUtc.HasValue)
        };

        return fields.Select(field =>
        {
            var recoveredCount = vehicles.Count(field.HasValue);
            var status = recoveredCount == 0
                ? vehicles.Count == 0 ? TeslaCoverageStatus.Missing : TeslaCoverageStatus.ManualEntryRequired
                : recoveredCount == vehicles.Count
                    ? TeslaCoverageStatus.Recovered
                    : TeslaCoverageStatus.Partial;

            return new TeslaFieldCoverage(field.Name, status, recoveredCount, vehicles.Count);
        }).ToArray();
    }
}

public sealed class TeslaVehicleSnapshot
{
    public string Brand { get; init; } = "Tesla";
    public string Model { get; init; } = string.Empty;
    public string? Trim { get; init; }
    public double? PriceIncludingTax { get; init; }
    public double? PriceExcludingTax { get; init; }
    public string? Technology { get; init; }
    public int? PowerHp { get; init; }
    public string? Transmission { get; init; }
    public int? Seats { get; init; }
    public int? TrunkLiters { get; init; }
    public int? RangeKm { get; init; }
    public int? RangeWltpKm { get; init; }
    public string? ChargingInformation { get; init; }
    public int? Co2GramsPerKm { get; init; }
    public double? BatteryCapacityKwh { get; init; }
    public double? AccelerationSeconds { get; init; }
    public int? ChargingStartPercent { get; init; }
    public int? ChargingEndPercent { get; init; }
    public int? ChargingTimeMinutes { get; init; }
    public IReadOnlyList<string> Features { get; init; } = [];
    public string? ImageUrl { get; init; }
    public string? ManufacturerUrl { get; init; }
    public DateTime CollectedAtUtc { get; init; }
    public DateTime? LastUpdatedAtUtc { get; init; }
}

public sealed record TeslaParseResult(
    Uri Source,
    DateTime CollectedAtUtc,
    IReadOnlyList<TeslaVehicleSnapshot> Vehicles,
    IReadOnlyList<TeslaFieldCoverage> Coverage);

public sealed record TeslaFieldCoverage(
    string Field,
    TeslaCoverageStatus Status,
    int RecoveredVehicleCount,
    int VehicleCount);

public enum TeslaCoverageStatus
{
    Recovered,
    Partial,
    Missing,
    ManualEntryRequired
}