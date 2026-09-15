using System.Net;
using System.Text.RegularExpressions;
using ChooseYourCar.Parsing.Tesla;

namespace ChooseYourCar.Parsing.Renault;

public sealed class RenaultParser(HttpClient httpClient)
{
    private static readonly string[] SupportedModels =
    [
        "Twingo", "Renault 5", "Renault 4", "Megane E-Tech", "Scenic", "Kangoo", "Grand Kangoo",
        "Clio", "Captur", "Symbioz", "Austral", "Espace", "Rafale"
    ];
    private static readonly Regex ModelRegex = new(@"\b(?:Twingo|Renault\s+5|Renault\s+4|Megane\s+E-Tech|Scenic|Grand\s+Kangoo|Kangoo|Clio|Captur|Symbioz|Austral|Espace|Rafale)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex LinkRegex = new(@"https?://www\.renault\.fr/[^""'\s<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ImageRegex = new(@"https?://[^""'\s<>]+\.(?:jpg|jpeg|png|webp)(?:\?[^""'\s<>]*)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ConfiguratorLinkRegex = new(@"https?://www\.renault\.fr/[^""'\s<>]*configurateur[^""'\s<>]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public async Task<TeslaParseResult> ParseAsync(Uri source, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(source, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var electricHtml = await GetPageAsync(new Uri(source, "e-tech-electrique.html"), cancellationToken);
        var hybridHtml = await GetPageAsync(new Uri(source, "vehicules-hybrides.html"), cancellationToken);
        var result = ParseHtml(source, html + electricHtml + hybridHtml, DateTime.UtcNow);
        var enrichedVehicles = new List<TeslaVehicleSnapshot>(result.Vehicles.Count);

        foreach (var vehicle in result.Vehicles)
        {
            enrichedVehicles.Add(await EnrichFromDetailPageAsync(vehicle, cancellationToken));
        }

        return result with
        {
            Vehicles = enrichedVehicles,
            Coverage = BuildCoverageReport(enrichedVehicles)
        };
    }

    private async Task<string> GetPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync(cancellationToken)
            : string.Empty;
    }

    private async Task<TeslaVehicleSnapshot> EnrichFromDetailPageAsync(
        TeslaVehicleSnapshot vehicle,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(vehicle.ManufacturerUrl, UriKind.Absolute, out var detailUri))
        {
            return vehicle;
        }

        using var response = await httpClient.GetAsync(detailUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return vehicle;
        }

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(cancellationToken));
        var configuratorLink = ConfiguratorLinkRegex.Match(html);
        if (configuratorLink.Success)
        {
            html += await GetPageAsync(new Uri(configuratorLink.Value), cancellationToken);
        }
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ");
        var technology = vehicle.Model switch
        {
            "Twingo" or "Renault 5" or "Renault 4" or "Megane E-Tech" or "Scenic" or "Kangoo" or "Grand Kangoo" => "Electrique",
            _ => "Hybride"
        };
        var power = MatchInt(text, @"\b([1-9]\d{1,3})\s*ch\b");
        var price = MatchPrice(text);
        var range = technology == "Electrique" ? MatchInt(text, @"autonomie(?:\s+étendue)?\s+jusqu[’']à\s*([1-9]\d{2,3})\s*km") : null;
        var totalRange = vehicle.Model switch
        {
            "Captur" => 1500,
            "Clio" => 1000,
            "Symbioz" => 1000,
            "Austral" => 1000,
            "Espace" => 1000,
            "Rafale" => 1000,
            _ => (int?)null
        };
        var trunk = MatchInt(text, @"volume de coffre\s+(?:jusqu[’']à\s+)?([1-9]\d{1,3})\s*L\b");
        var co2 = MatchInt(text, @"(?:émissions|emissions)\s+(?:de\s+)?CO2[^\d]{0,80}([1-9]\d{1,2})\s*g/km");
        var image = ImageRegex.Match(html);

        return new TeslaVehicleSnapshot
        {
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Trim = vehicle.Trim,
            PriceIncludingTax = price ?? vehicle.PriceIncludingTax,
            PriceExcludingTax = vehicle.PriceExcludingTax,
            Technology = technology ?? vehicle.Technology,
            PowerHp = power ?? vehicle.PowerHp,
            Transmission = html.Contains("boîte de vitesses", StringComparison.OrdinalIgnoreCase) && html.Contains("automatique", StringComparison.OrdinalIgnoreCase) ? "Automatique" : vehicle.Transmission,
            Seats = 5,
            TrunkLiters = trunk ?? vehicle.TrunkLiters,
            RangeKm = totalRange ?? vehicle.RangeKm,
            RangeWltpKm = range ?? vehicle.RangeWltpKm,
            Co2GramsPerKm = co2 ?? vehicle.Co2GramsPerKm,
            ChargingInformation = vehicle.ChargingInformation,
            Features = vehicle.Features,
            ImageUrl = vehicle.ImageUrl ?? (image.Success ? image.Value : null),
            ManufacturerUrl = vehicle.ManufacturerUrl,
            CollectedAtUtc = vehicle.CollectedAtUtc,
            LastUpdatedAtUtc = vehicle.LastUpdatedAtUtc
        };
    }

    private static int? MatchInt(string value, string pattern)
    {
        var match = Regex.Match(value, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var result) ? result : null;
    }

    private static double? MatchPrice(string value)
    {
        var match = Regex.Match(value, @"prix\s+comptant[\s\S]{0,120}?à\s+partir\s+de\s+([0-9\s\u00A0]+)\s*€", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var digits = match.Success ? new string(match.Groups[1].Value.Where(char.IsDigit).ToArray()) : string.Empty;
        return double.TryParse(digits, out var result) ? result : null;
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
        var link = LinkRegex.Match(context);
        var image = ImageRegex.Match(context);

        return new TeslaVehicleSnapshot
        {
            Brand = "Renault",
            Model = model,
            ManufacturerUrl = link.Success ? link.Value : new Uri(source, ModelSlug(model)).ToString(),
            ImageUrl = image.Success ? image.Value : null,
            CollectedAtUtc = DateTime.SpecifyKind(collectedAtUtc, DateTimeKind.Utc)
        };
    }

    private static string ModelSlug(string model) => model switch
    {
        "Twingo" => "vehicules-electriques/twingo-e-tech-electrique.html",
        "Renault 5" => "vehicules-electriques/r5-e-tech-electrique.html",
        "Renault 4" => "vehicules-electriques/r4-e-tech-electrique.html",
        "Megane E-Tech" => "vehicules-electriques/nouvelle-megane.html",
        "Scenic" => "vehicules-electriques/scenic-e-tech-electrique.html",
        "Kangoo" => "vehicules-electriques/kangoo-e-tech-electrique.html",
        "Grand Kangoo" => "vehicules-electriques/grand-kangoo-e-tech-electrique.html",
        "Clio" => "vehicules-hybrides/nouvelle-clio.html",
        "Captur" => "vehicules-hybrides/captur.html",
        "Symbioz" => "vehicules-hybrides/symbioz.html",
        "Austral" => "vehicules-hybrides/austral.html",
        "Espace" => "vehicules-hybrides/espace.html",
        "Rafale" => "vehicules-hybrides/rafale.html",
        _ => "trouver-votre-renault.html"
    };

    private static IReadOnlyList<TeslaFieldCoverage> BuildCoverageReport(IReadOnlyList<TeslaVehicleSnapshot> vehicles)
    {
        var recoveredImages = vehicles.Count(vehicle => !string.IsNullOrWhiteSpace(vehicle.ImageUrl));
        return
        [
            new("identite", vehicles.Count > 0 ? TeslaCoverageStatus.Recovered : TeslaCoverageStatus.Missing, vehicles.Count, vehicles.Count),
            new("url_constructeur", vehicles.All(vehicle => !string.IsNullOrWhiteSpace(vehicle.ManufacturerUrl)) ? TeslaCoverageStatus.Recovered : TeslaCoverageStatus.Partial, vehicles.Count(vehicle => !string.IsNullOrWhiteSpace(vehicle.ManufacturerUrl)), vehicles.Count),
            new("visuel_principal", recoveredImages == vehicles.Count ? TeslaCoverageStatus.Recovered : recoveredImages == 0 ? TeslaCoverageStatus.ManualEntryRequired : TeslaCoverageStatus.Partial, recoveredImages, vehicles.Count),
            new("prix_ttc", TeslaCoverageStatus.ManualEntryRequired, 0, vehicles.Count),
            new("caracteristiques", TeslaCoverageStatus.ManualEntryRequired, 0, vehicles.Count)
        ];
    }
}