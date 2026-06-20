using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Application.Common.Security;

namespace PrettyWoman.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var existingAdmins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        if (existingAdmins.Count > 0)
        {
            return;
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "No existe ningun usuario administrador. Debe configurar SeedAdmin:Email y SeedAdmin:Password para inicializar el sistema.");
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = configuration["SeedAdmin:Name"] ?? "Admin",
                Lastname = configuration["SeedAdmin:Lastname"] ?? "Principal"
            };

            var createResult = await userManager.CreateAsync(admin, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ", createResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}
