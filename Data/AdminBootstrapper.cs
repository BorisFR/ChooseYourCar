using Microsoft.AspNetCore.Identity;

namespace ChooseYourCar.Data;

public static class AdminBootstrapper
{
    private const string AdministratorRole = "Administrator";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminBootstrapper");
        var email = configuration["AdminBootstrap:Email"] ?? "admin@chooseyourcar.local";
        var password = configuration["AdminBootstrap:Password"] ?? configuration["ADMIN_BOOTSTRAP_PASSWORD"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin bootstrap skipped because no bootstrap password is configured.");
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(AdministratorRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdministratorRole));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create administrator role: {FormatErrors(roleResult)}");
            }
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var userResult = await userManager.CreateAsync(user, password);
            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create administrator account: {FormatErrors(userResult)}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, AdministratorRole))
        {
            var roleResult = await userManager.AddToRoleAsync(user, AdministratorRole);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Unable to assign administrator role: {FormatErrors(roleResult)}");
            }
        }
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}