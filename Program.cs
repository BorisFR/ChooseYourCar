using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using ChooseYourCar.Components;
using ChooseYourCar.Components.Account;
using ChooseYourCar.Data;
using ChooseYourCar.Parsing.Tesla;
using ChooseYourCar.Parsing.Renault;
using ChooseYourCar.Parsing.Peugeot;
using ChooseYourCar.Parsing.Byd;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddHttpClient<TeslaParser>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ChooseYourCar prototype parser/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9,en;q=0.8");
    client.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<TeslaVehicleImporter>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ChooseYourCar prototype parser/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9,en;q=0.8");
    client.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<RenaultParser>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ChooseYourCar prototype parser/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9,en;q=0.8");
    client.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<PeugeotParser>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ChooseYourCar prototype parser/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9,en;q=0.8");
    client.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<BydParser>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ChooseYourCar prototype parser/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9,en;q=0.8");
    client.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    VehicleCatalog.Seed(db);
    await AdminBootstrapper.SeedAsync(scope.ServiceProvider, builder.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/prototype/tesla-parser", async (TeslaParser parser, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await parser.ParseAsync(new Uri("https://www.tesla.com/fr_fr/"), cancellationToken);
            return Results.Ok(result);
        }
        catch (HttpRequestException exception)
        {
            return Results.Problem(
                detail: $"La page Tesla n'est pas accessible depuis l'environnement courant : {exception.Message}",
                statusCode: StatusCodes.Status502BadGateway,
                title: "Import Tesla indisponible");
        }
    });

    app.MapPost("/prototype/tesla-parser/import", async (
        TeslaParser parser,
        TeslaVehicleImporter importer,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var parseResult = await parser.ParseAsync(new Uri("https://www.tesla.com/fr_fr/"), cancellationToken);
            var importResult = await importer.ImportAsync(parseResult, cancellationToken);
            return Results.Ok(new { importResult, parseResult.Coverage });
        }
        catch (HttpRequestException exception)
        {
            return Results.Problem(
                detail: $"La page Tesla n'est pas accessible depuis l'environnement courant : {exception.Message}",
                statusCode: StatusCodes.Status502BadGateway,
                title: "Import Tesla indisponible");
        }
    });

    app.MapPost("/prototype/renault-parser/import", async (
        RenaultParser parser,
        TeslaVehicleImporter importer,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var parseResult = await parser.ParseAsync(new Uri("https://www.renault.fr/"), cancellationToken);
            var importResult = await importer.ImportAsync(parseResult, cancellationToken);
            return Results.Ok(new { importResult, parseResult.Coverage, parseResult.Vehicles });
        }
        catch (HttpRequestException exception)
        {
            return Results.Problem(
                detail: $"La page Renault n'est pas accessible depuis l'environnement courant : {exception.Message}",
                statusCode: StatusCodes.Status502BadGateway,
                title: "Import Renault indisponible");
        }
    });

    app.MapPost("/prototype/peugeot-parser/import", async (
        PeugeotParser parser,
        TeslaVehicleImporter importer,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var parseResult = await parser.ParseAsync(new Uri("https://www.peugeot.fr/"), cancellationToken);
            var importResult = await importer.ImportAsync(parseResult, cancellationToken);
            return Results.Ok(new { importResult, parseResult.Coverage, parseResult.Vehicles });
        }
        catch (HttpRequestException exception)
        {
            return Results.Problem(
                detail: $"La page Peugeot n'est pas accessible depuis l'environnement courant : {exception.Message}",
                statusCode: StatusCodes.Status502BadGateway,
                title: "Import Peugeot indisponible");
        }
    });

    app.MapPost("/prototype/byd-parser/import", async (
        BydParser parser,
        TeslaVehicleImporter importer,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var parseResult = await parser.ParseAsync(new Uri("https://www.byd.com/fr"), cancellationToken);
            var importResult = await importer.ImportAsync(parseResult, cancellationToken);
            return Results.Ok(new { importResult, parseResult.Coverage, parseResult.Vehicles });
        }
        catch (HttpRequestException exception)
        {
            return Results.Problem(detail: $"La page BYD n'est pas accessible : {exception.Message}", statusCode: 502, title: "Import BYD indisponible");
        }
    });

    app.MapPost("/prototype/byd-parser/sample/import", async (
        TeslaVehicleImporter importer,
        ApplicationDbContext db,
        CancellationToken cancellationToken) =>
    {
        const string sampleHtml = """
            <h2>BYD DOLPHIN G DM-i</h2><a href="https://www.byd.com/fr/voitures-hybrides/dolphin-g-dm-i">En savoir plus</a>
            <h2>BYD ATTO 2 DM-i</h2><a href="https://www.byd.com/fr/voitures-hybrides/atto-2-dm-i">En savoir plus</a>
            <h2>BYD ATTO 3 EVO</h2><a href="https://www.byd.com/fr/voitures-electriques/atto-3-evo">En savoir plus</a>
            <h2>BYD SEALION 5 DM-i</h2><a href="https://www.byd.com/fr/voitures-hybrides/sealion-5-dm-i">En savoir plus</a>
            <h2>BYD DOLPHIN SURF</h2><a href="https://www.byd.com/fr/voitures-electriques/dolphin-surf">En savoir plus</a>
            <h2>BYD DOLPHIN</h2><a href="https://www.byd.com/fr/voitures-electriques/dolphin">En savoir plus</a>
            <h2>BYD SEAL</h2><a href="https://www.byd.com/fr/voitures-electriques/seal">En savoir plus</a>
            <h2>BYD SEALION 7</h2><a href="https://www.byd.com/fr/voitures-electriques/sealion-7">En savoir plus</a>
            <h2>BYD SEAL U</h2><a href="https://www.byd.com/fr/voitures-electriques/seal-u">En savoir plus</a>
            <h2>BYD TANG</h2><a href="https://www.byd.com/fr/voitures-electriques/tang">En savoir plus</a>
            <h2>BYD SEAL 6 DM-i</h2><a href="https://www.byd.com/fr/vehicules-hybrides/seal-6-dm-i">En savoir plus</a>
            <h2>BYD SEAL 6 DM-i TOURING</h2><a href="https://www.byd.com/fr/vehicules-hybrides/seal-6-dm-i-touring">En savoir plus</a>
            <h2>BYD SEAL U DM-i</h2><a href="https://www.byd.com/fr/vehicules-hybrides/seal-u-dm-i">En savoir plus</a>
            <h2>BYD SHARK DMO</h2><a href="https://www.byd.com/fr/voitures-hybrides/shark-dmo">En savoir plus</a>
            """;
        var parseResult = BydParser.ParseHtml(new Uri("https://www.byd.com/fr"), sampleHtml, DateTime.UtcNow);
        var importResult = await importer.ImportAsync(parseResult, cancellationToken);
        var vehicles = await db.Vehicles.AsNoTracking().Where(vehicle => vehicle.Brand == "BYD").OrderBy(vehicle => vehicle.Model).Select(vehicle => new { vehicle.Model, vehicle.Status, vehicle.Technology }).ToListAsync(cancellationToken);
        return Results.Ok(new { importResult, vehicles });
    });

    app.MapPost("/prototype/peugeot-parser/sample/import", async (
        TeslaVehicleImporter importer,
        ApplicationDbContext db,
        CancellationToken cancellationToken) =>
    {
        const string sampleHtml = """
            <h2>Nouvelle E-208 GTi</h2><a href="https://www.peugeot.fr/nos-vehicules/nouvelle-208-gti.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/white-label-new-nav/2025/mob-208.png"></a>
            <h2>208</h2><a href="https://www.peugeot.fr/nos-vehicules/208/hybride-essence.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_208.png"></a>
            <h2>2008</h2><a href="https://www.peugeot.fr/nos-vehicules/suv-peugeot-2008/hybride-essence.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_2008.png"></a>
            <h2>Nouvelle 308</h2><a href="https://www.peugeot.fr/nos-vehicules/nouvelle-peugeot-308/hybride.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_NEW_308.png"></a>
            <h2>Nouvelle 308 SW</h2><a href="https://www.peugeot.fr/nos-vehicules/nouvelle-peugeot-308-sw/hybride.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_NEW_308_SW.png"></a>
            <h2>3008</h2><a href="https://www.peugeot.fr/nos-vehicules/peugeot-3008/hybride.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_3008.png"></a>
            <h2>Nouvelle 408</h2><a href="https://www.peugeot.fr/nos-vehicules/nouvelle-peugeot-408/electrique.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_NEW_408.png"></a>
            <h2>5008</h2><a href="https://www.peugeot.fr/nos-vehicules/peugeot-5008/electrique.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_5008.png"></a>
            <h2>RIFTER</h2><a href="https://www.peugeot.fr/nos-vehicules/e-rifter.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_RIFTER.png"></a>
            <h2>TRAVELLER</h2><a href="https://www.peugeot.fr/nos-vehicules/nouveau-e-traveller.html"><img src="https://www.peugeot.fr/content/dam/peugeot/master/b2c/our-range/model-thumbnails/homepage/HP_RANGE_TRAVELLER.png"></a>
            """;
        var parseResult = PeugeotParser.ParseHtml(new Uri("https://www.peugeot.fr/"), sampleHtml, DateTime.UtcNow);
        var importResult = await importer.ImportAsync(parseResult, cancellationToken);
        var vehicles = await db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.Brand == "Peugeot")
            .OrderBy(vehicle => vehicle.Model)
            .Select(vehicle => new { vehicle.Model, vehicle.ImageUrl, vehicle.Status, vehicle.ImportSource })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { importResult, vehicles });
    });

    app.MapGet("/prototype/tesla-parser/sample", () =>
    {
        const string sampleHtml = """
            <h2>Model Y</h2><p>À partir de 32 390 €</p><a href="https://www.tesla.com/fr_fr/modely/design">Commander</a><img src="https://digitalassets.tesla.com/tesla-contents/image/upload/f_auto,q_auto/Homepage-Card-Model-Y-Desktop-EMEA.jpg">
            <h2>Model 3</h2><p>À partir de 33 590 €</p><a href="https://www.tesla.com/fr_fr/model3/design">Commander</a><img src="https://digitalassets.tesla.com/tesla-contents/image/upload/f_auto,q_auto/Homepage-Promo-Model-3-Desktop-EMEA-LHD.jpg">
            <h2>Model S</h2><a href="https://www.tesla.com/fr_fr/models/design">Commander</a>
            <h2>Model X</h2><a href="https://www.tesla.com/fr_fr/modelx/design">Commander</a>
            <h2>Cybertruck</h2><a href="https://www.tesla.com/fr_fr/cybertruck/design">Commander</a><img src="https://digitalassets.tesla.com/tesla-contents/image/upload/f_auto,q_auto/Cybertruck-Main-Hero-Desktop.jpg">
            """;

        return Results.Ok(TeslaParser.ParseHtml(
            new Uri("https://www.tesla.com/fr_fr/"),
            sampleHtml,
            DateTime.UtcNow));
    });

    app.MapPost("/prototype/tesla-parser/sample/import", async (
        TeslaVehicleImporter importer,
        ApplicationDbContext db,
        CancellationToken cancellationToken) =>
    {
        const string sampleHtml = """
            <h2>Model Y</h2><p>À partir de 32 390 €</p><a href="https://www.tesla.com/fr_fr/modely/design">Commander</a><img src="https://digitalassets.tesla.com/tesla-contents/image/upload/f_auto,q_auto/Homepage-Card-Model-Y-Desktop-EMEA.jpg">
            <h2>Model 3</h2><p>À partir de 33 590 €</p><a href="https://www.tesla.com/fr_fr/model3/design">Commander</a><img src="https://digitalassets.tesla.com/tesla-contents/image/upload/f_auto,q_auto/Homepage-Promo-Model-3-Desktop-EMEA-LHD.jpg">
            <h2>Model S</h2><a href="https://www.tesla.com/fr_fr/models/design">Commander</a>
            <h2>Model X</h2><a href="https://www.tesla.com/fr_fr/modelx/design">Commander</a>
            <h2>Cybertruck</h2><a href="https://www.tesla.com/fr_fr/cybertruck/design">Commander</a><img src="https://digitalassets.tesla.com/tesla-contents/image/upload/f_auto,q_auto/Cybertruck-Main-Hero-Desktop.jpg">
            """;
        var parseResult = TeslaParser.ParseHtml(new Uri("https://www.tesla.com/fr_fr/"), sampleHtml, DateTime.UtcNow);
        var importResult = await importer.ImportAsync(parseResult, cancellationToken);
        var vehicles = await db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.Brand == "Tesla")
            .OrderBy(vehicle => vehicle.Model)
            .Select(vehicle => new { vehicle.Model, vehicle.PriceIncludingTax, vehicle.ImageUrl, vehicle.Status, vehicle.ImportSource, vehicle.CollectedAtUtc })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { importResult, vehicles });
    });
}

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
