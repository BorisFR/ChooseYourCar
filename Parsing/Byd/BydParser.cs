using System.Net;
using System.Text.RegularExpressions;
using ChooseYourCar.Parsing.Tesla;

namespace ChooseYourCar.Parsing.Byd;

public sealed class BydParser(HttpClient httpClient)
{
    private static readonly string[] SupportedModels =
    [
        "DOLPHIN G DM-i", "ATTO 2 DM-i", "ATTO 3 EVO", "SEALION 5 DM-i", "DOLPHIN SURF",
        "DOLPHIN", "SEAL", "SEALION 7", "SEAL U", "TANG", "SEAL 6 DM-i",
        "SEAL 6 DM-i TOURING", "SEAL U DM-i", "SHARK DMO"
    ];
    private static readonly Regex ModelRegex = new(@"\b(?:DOLPHIN\s+G\s+DM-i|ATTO\s+2\s+DM-i|ATTO\s+3\s+EVO|SEALION\s+5\s+DM-i|DOLPHIN\s+SURF|DOLPHIN|SEALION\s+7|SEAL\s+6\s+DM-i\s+TOURING|SEAL\s+6\s+DM-i|SEAL\s+U\s+DM-i|SEAL\s+U|ATTO\s+3|ATTO\s+2|SEAL|TANG|SHARK\s+DMO)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex LinkRegex = new(@"https?://www\.byd\.com/fr/[^""'\s<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ImageRegex = new(@"https?://www\.byd\.com/[^""'\s<>]+\.(?:jpg|jpeg|png|webp)(?:\?[^""'\s<>]*)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public async Task<TeslaParseResult> ParseAsync(Uri source, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(source, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = ParseHtml(source, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(cancellationToken)), DateTime.UtcNow);
        var enrichedVehicles = new List<TeslaVehicleSnapshot>(result.Vehicles.Count);

        foreach (var vehicle in result.Vehicles)
        {
            enrichedVehicles.Add(await EnrichAsync(vehicle, cancellationToken));
        }

        return result with { Vehicles = enrichedVehicles, Coverage = BuildCoverageReport(enrichedVehicles) };
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
        var configuratorHtml = await GetConfiguratorHtmlAsync(vehicle.Model, cancellationToken);
        html += configuratorHtml;
        var text = Regex.Replace(Regex.Replace(html, "<[^>]+>", " "), @"\s+", " ");
        var power = MatchInt(text, @"\b([1-9]\d{1,3})\s*(?:ch|CV)\b");
        var range = vehicle.Technology == "Electrique"
            ? MatchInt(text, "\"val\"\\s*:\\s*\"([1-9]\\d{2,3})\"[^}]{0,100}?\"bottom\"\\s*:\\s*\"Autonomie en cycle WLTP")
            : null;
        var battery = MatchDouble(text, "\"val\"\\s*:\\s*\"([0-9]+(?:[,.][0-9]+)?)\"[^}]{0,100}?(?:\"suffix\"\\s*:\\s*\"kWh|\"bottom\"\\s*:\\s*\"Capacité)");
        var acceleration = MatchDouble(text, "\"val\"\\s*:\\s*\"([0-9]+(?:[,.][0-9]+)?)\"[^}]{0,100}?\"bottom\"\\s*:\\s*\"0 à 100 km/h");
        var charging = MatchCharging(text);
        var price = MatchPrice(text) ?? MatchConfiguratorPrice(text);
        var image = ImageRegex.Match(html);

        return new TeslaVehicleSnapshot
        {
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            PriceIncludingTax = price ?? vehicle.PriceIncludingTax,
            Technology = vehicle.Technology,
            PowerHp = power ?? vehicle.PowerHp,
            Transmission = text.Contains("automatique", StringComparison.OrdinalIgnoreCase) ? "Automatique" : vehicle.Transmission,
            Seats = MatchInt(text, @"\b([1-9])\s*places?\b") ?? vehicle.Seats,
            TrunkLiters = MatchInt(text, @"(?:coffre|chargement)[^\d]{0,80}([1-9]\d{2,3})\s*L") ?? vehicle.TrunkLiters,
            RangeWltpKm = range ?? vehicle.RangeWltpKm,
            Co2GramsPerKm = MatchInt(text, @"CO2[^\d]{0,50}([1-9]\d{1,2})\s*g/km") ?? vehicle.Co2GramsPerKm,
            BatteryCapacityKwh = battery ?? vehicle.BatteryCapacityKwh,
            AccelerationSeconds = acceleration ?? vehicle.AccelerationSeconds,
            ChargingStartPercent = charging.Start ?? vehicle.ChargingStartPercent,
            ChargingEndPercent = charging.End ?? vehicle.ChargingEndPercent,
            ChargingTimeMinutes = charging.Minutes ?? vehicle.ChargingTimeMinutes,
            ImageUrl = vehicle.ImageUrl ?? (image.Success ? image.Value : null),
            ManufacturerUrl = vehicle.ManufacturerUrl,
            CollectedAtUtc = vehicle.CollectedAtUtc
        };
    }

    private async Task<string> GetConfiguratorHtmlAsync(string model, CancellationToken cancellationToken)
    {
        var slug = ConfiguratorSlug(model);
        if (slug is null)
        {
            return string.Empty;
        }

        using var response = await httpClient.GetAsync(new Uri($"https://www.byd.com/fr/configurateur/{slug}"), cancellationToken);
        return response.IsSuccessStatusCode
            ? WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(cancellationToken))
            : string.Empty;
    }

    private static string? ConfiguratorSlug(string model) => model switch
    {
        "DOLPHIN G DM-i" => "dolphin-g-dm-i",
        "ATTO 2 DM-i" => "atto-2-dm-i",
        "ATTO 3 EVO" => "atto-3-evo",
        "SEALION 5 DM-i" => "sealion-5-dm-i",
        "DOLPHIN SURF" => "dolphin-surf",
        "DOLPHIN" => "dolphin",
        "SEAL" => "seal",
        "SEALION 7" => "sealion-7",
        "SEAL U" => "seal-u",
        "TANG" => "tang",
        "SEAL 6 DM-i" => "seal-6-dm-i",
        "SEAL 6 DM-i TOURING" => "seal-6-dm-i-touring",
        "SEAL U DM-i" => "seal-u-dm-i",
        "SHARK DMO" => "shark-dmo",
        _ => null
    };

    private static int? MatchInt(string value, string pattern)
    {
        var match = Regex.Match(value, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var result) ? result : null;
    }

    private static double? MatchDouble(string value, string pattern)
    {
        var match = Regex.Match(value, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var normalized = match.Groups[1].Value.Replace(',', '.');
        return double.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static (int? Start, int? End, int? Minutes) MatchCharging(string value)
    {
        var match = Regex.Match(value, @"(?:recharge|passer)[^\d]{0,80}(\d{1,3})\s*%\s*(?:à|-|–)\s*(\d{1,3})\s*%[^\d]{0,40}(\d{1,3})\s*(?:min|minutes)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var start) && int.TryParse(match.Groups[2].Value, out var end) && int.TryParse(match.Groups[3].Value, out var minutes)
            ? (start, end, minutes)
            : (null, null, null);
    }

    private static double? MatchPrice(string value)
    {
        var match = Regex.Match(value, @"prix\s+(?:comptant|catalogue)[\s\S]{0,120}?à\s+partir\s+de\s+([0-9\s\u00A0]+)\s*€", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var digits = match.Success ? new string(match.Groups[1].Value.Where(char.IsDigit).ToArray()) : "";
        return double.TryParse(digits, out var result) ? result : null;
    }

    private static double? MatchConfiguratorPrice(string value)
    {
        var match = Regex.Match(value, @"(?:Active|Boost|Comfort|Design|Excellence)\s+([0-9\s\u00A0]+)\s*€", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var digits = match.Success ? new string(match.Groups[1].Value.Where(char.IsDigit).ToArray()) : "";
        return double.TryParse(digits, out var result) ? result : null;
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
        var isElectric = !model.Contains("DM-i", StringComparison.OrdinalIgnoreCase) && !model.Equals("SHARK DMO", StringComparison.OrdinalIgnoreCase);

        return new TeslaVehicleSnapshot
        {
            Brand = "BYD",
            Model = model,
            Technology = isElectric ? "Electrique" : "Hybride rechargeable",
            ManufacturerUrl = link.Success ? link.Value : $"https://www.byd.com/fr/{ModelSlug(model)}",
            ImageUrl = image.Success ? image.Value : null,
            CollectedAtUtc = DateTime.SpecifyKind(collectedAtUtc, DateTimeKind.Utc)
        };
    }

    private static string ModelSlug(string model) => $"voitures-electriques/{model.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal)}";

    private static IReadOnlyList<TeslaFieldCoverage> BuildCoverageReport(IReadOnlyList<TeslaVehicleSnapshot> vehicles) =>
    [
        new("identite", vehicles.Count > 0 ? TeslaCoverageStatus.Recovered : TeslaCoverageStatus.Missing, vehicles.Count, vehicles.Count),
        new("url_constructeur", TeslaCoverageStatus.Recovered, vehicles.Count, vehicles.Count),
        new("technologie", vehicles.Count > 0 ? TeslaCoverageStatus.Recovered : TeslaCoverageStatus.Missing, vehicles.Count, vehicles.Count),
        new("prix_ttc", TeslaCoverageStatus.ManualEntryRequired, 0, vehicles.Count),
        new("caracteristiques", TeslaCoverageStatus.ManualEntryRequired, 0, vehicles.Count)
    ];
}