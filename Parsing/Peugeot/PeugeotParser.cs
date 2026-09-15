using System.Net;
using System.Text.RegularExpressions;
using ChooseYourCar.Parsing.Tesla;

namespace ChooseYourCar.Parsing.Peugeot;

public sealed class PeugeotParser(HttpClient httpClient)
{
    private static readonly string[] SupportedModels =
    [
        "E-208 GTi", "208", "2008", "308 SW", "308", "3008", "408", "5008", "Rifter", "Traveller"
    ];
    private static readonly Regex ModelRegex = new(@"\b(?:E-208\s+GTi|208|2008|308\s+SW|308|3008|408|5008|Rifter|Traveller)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex LinkRegex = new(@"https?://www\.peugeot\.fr/[^""'\s<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ImageRegex = new(@"https?://www\.peugeot\.fr/[^""'\s<>]+\.(?:jpg|jpeg|png|webp)(?:\?[^""'\s<>]*)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ConfiguratorLinkRegex = new(@"https?://www\.peugeot\.fr/[^""'\s<>]*configurateur[^""'\s<>]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public async Task<TeslaParseResult> ParseAsync(Uri source, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(source, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(cancellationToken));
        var result = ParseHtml(source, html, DateTime.UtcNow);
        var enriched = new List<TeslaVehicleSnapshot>(result.Vehicles.Count);

        foreach (var vehicle in result.Vehicles)
        {
            enriched.Add(await EnrichAsync(vehicle, cancellationToken));
        }

        return result with { Vehicles = enriched, Coverage = BuildCoverageReport(enriched) };
    }

    private async Task<TeslaVehicleSnapshot> EnrichAsync(TeslaVehicleSnapshot vehicle, CancellationToken cancellationToken)
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
            try
            {
                using var configuratorResponse = await httpClient.GetAsync(new Uri(configuratorLink.Value), cancellationToken);
                if (configuratorResponse.IsSuccessStatusCode)
                {
                    html += WebUtility.HtmlDecode(await configuratorResponse.Content.ReadAsStringAsync(cancellationToken));
                }
            }
            catch (HttpRequestException)
            {
                // Keep the model page data when a configurator is unavailable.
            }
        }
        var text = Regex.Replace(Regex.Replace(html, "<[^>]+>", " "), @"\s+", " ");
        var technology = text.Contains("électrique", StringComparison.OrdinalIgnoreCase) ? "Electrique" : "Hybride";
        var power = MatchInt(text, @"\b([1-9]\d{1,3})\s*ch\b");
        var range = technology == "Electrique" ? MatchInt(text, @"autonomie[^\d]{0,80}([1-9]\d{2,3})\s*km") : null;
        var price = MatchPrice(text);
        var image = ImageRegex.Match(html);

        return new TeslaVehicleSnapshot
        {
            Brand = "Peugeot",
            Model = vehicle.Model,
            PriceIncludingTax = price ?? vehicle.PriceIncludingTax,
            Technology = technology,
            PowerHp = power ?? vehicle.PowerHp,
            Transmission = text.Contains("automatique", StringComparison.OrdinalIgnoreCase) ? "Automatique" : vehicle.Transmission,
            Seats = 5,
            RangeWltpKm = range ?? vehicle.RangeWltpKm,
            ImageUrl = vehicle.ImageUrl ?? (image.Success ? image.Value : null),
            ManufacturerUrl = vehicle.ManufacturerUrl,
            CollectedAtUtc = vehicle.CollectedAtUtc
        };
    }

    public static TeslaParseResult ParseHtml(Uri source, string html, DateTime collectedAtUtc)
    {
        var normalized = WebUtility.HtmlDecode(html);
        var vehicles = SupportedModels
            .Select(model => ParseVehicle(model, source, normalized, collectedAtUtc))
            .Where(vehicle => vehicle is not null)
            .Cast<TeslaVehicleSnapshot>()
            .ToArray();

        return new TeslaParseResult(source, collectedAtUtc, vehicles, BuildCoverageReport(vehicles));
    }

    private static TeslaVehicleSnapshot? ParseVehicle(string model, Uri source, string html, DateTime collectedAtUtc)
    {
        var match = Regex.Match(html, $@"\b{Regex.Escape(model)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var next = ModelRegex.Match(html, match.Index + match.Length);
        var length = next.Success ? next.Index - match.Index : html.Length - match.Index;
        var context = html.Substring(match.Index, length);
        var link = LinkRegex.Match(context);
        var image = ImageRegex.Match(context);

        return new TeslaVehicleSnapshot
        {
            Brand = "Peugeot",
            Model = model,
            ManufacturerUrl = link.Success ? link.Value : new Uri(source, ModelSlug(model)).ToString(),
            ImageUrl = image.Success ? image.Value : null,
            CollectedAtUtc = DateTime.SpecifyKind(collectedAtUtc, DateTimeKind.Utc)
        };
    }

    private static string ModelSlug(string model) => model switch
    {
        "E-208 GTi" or "208" => "nos-vehicules/208/hybride-essence.html",
        "2008" => "nos-vehicules/suv-peugeot-2008/hybride-essence.html",
        "308" => "nos-vehicules/nouvelle-peugeot-308/hybride.html",
        "308 SW" => "nos-vehicules/nouvelle-peugeot-308-sw/hybride.html",
        "3008" => "nos-vehicules/peugeot-3008/hybride.html",
        "408" => "nos-vehicules/nouvelle-peugeot-408/electrique.html",
        "5008" => "nos-vehicules/peugeot-5008/electrique.html",
        "Rifter" => "nos-vehicules/e-rifter.html",
        "Traveller" => "nos-vehicules/nouveau-e-traveller.html",
        _ => ""
    };

    private static int? MatchInt(string value, string pattern)
    {
        var match = Regex.Match(value, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var result) ? result : null;
    }

    private static double? MatchPrice(string value)
    {
        var match = Regex.Match(value, @"prix\s+comptant[\s\S]{0,120}?à\s+partir\s+de\s+([0-9\s\u00A0]+)\s*€", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var digits = match.Success ? new string(match.Groups[1].Value.Where(char.IsDigit).ToArray()) : "";
        return double.TryParse(digits, out var result) ? result : null;
    }

    private static IReadOnlyList<TeslaFieldCoverage> BuildCoverageReport(IReadOnlyList<TeslaVehicleSnapshot> vehicles)
    {
        return
        [
            new("identite", vehicles.Count > 0 ? TeslaCoverageStatus.Recovered : TeslaCoverageStatus.Missing, vehicles.Count, vehicles.Count),
            new("url_constructeur", TeslaCoverageStatus.Recovered, vehicles.Count, vehicles.Count),
            new("prix_ttc", vehicles.Any(vehicle => vehicle.PriceIncludingTax.HasValue) ? TeslaCoverageStatus.Partial : TeslaCoverageStatus.ManualEntryRequired, vehicles.Count(vehicle => vehicle.PriceIncludingTax.HasValue), vehicles.Count),
            new("technologie", vehicles.Any(vehicle => vehicle.Technology is not null) ? TeslaCoverageStatus.Partial : TeslaCoverageStatus.ManualEntryRequired, vehicles.Count(vehicle => vehicle.Technology is not null), vehicles.Count),
            new("autonomie_wltp", vehicles.Any(vehicle => vehicle.RangeWltpKm.HasValue) ? TeslaCoverageStatus.Partial : TeslaCoverageStatus.ManualEntryRequired, vehicles.Count(vehicle => vehicle.RangeWltpKm.HasValue), vehicles.Count)
        ];
    }
}