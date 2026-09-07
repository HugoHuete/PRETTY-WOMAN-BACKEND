using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Infrastructure.Authentication;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task GetUsersAsync_FiltersByUserRoleAndEnabledStatus()
    {
        await using var serviceProvider = CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
        await roleManager.CreateAsync(new IdentityRole(AppRoles.Employee));
        var enabledEmployee = await CreateUserAsync(userManager, "maria.enabled", "María", "Habilitada", true, AppRoles.Employee);
        await CreateUserAsync(userManager, "maria.disabled", "María", "Deshabilitada", false, AppRoles.Employee);
        await CreateUserAsync(userManager, "maria.admin", "María", "Administradora", true, AppRoles.Admin);
        var authService = new AuthService(userManager, Options.Create(new JwtOptions
        {
            Key = "test-key-that-is-longer-than-thirty-two-characters",
            Issuer = "PrettyWoman.Tests",
            Audience = "PrettyWoman.Tests"
        }), context, null!);

        var users = await authService.GetUsersAsync(user: "maría", role: AppRoles.Employee, enabled: true);

        var user = Assert.Single(users);
        Assert.Equal(enabledEmployee.Id, user.Id);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<User>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        return services.BuildServiceProvider();
    }

    private static async Task<User> CreateUserAsync(
        UserManager<User> userManager,
        string username,
        string name,
        string lastname,
        bool enabled,
        string role)
    {
        var user = new User
        {
            UserName = username,
            Email = $"{username}@prettywoman.test",
            Name = name,
            Lastname = lastname,
            Enabled = enabled
        };

        var createResult = await userManager.CreateAsync(user, "ClavePrueba123!");
        var roleResult = await userManager.AddToRoleAsync(user, role);

        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(error => error.Description)));
        Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(error => error.Description)));
        return user;
    }
}
