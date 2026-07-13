using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PrettyWoman.Api.IntegrationTests.Infrastructure;

public sealed class PrettyWomanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin.integration@prettywoman.test";
    public const string AdminPassword = "Admin123!Integration";
    public const string EmployeeEmail = "employee.integration@prettywoman.test";
    public const string EmployeePassword = "Employee123!Integration";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pretty_woman_integration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    private readonly Dictionary<string, string?> _originalEnvironmentVariables = new();

    public PrettyWomanApiFactory()
    {
        SetEnvironmentVariable("Jwt__Key", "integration-tests-jwt-key-that-is-longer-than-thirty-two-characters");
        SetEnvironmentVariable("Jwt__Issuer", "PrettyWoman.IntegrationTests");
        SetEnvironmentVariable("Jwt__Audience", "PrettyWoman.IntegrationTests.Client");
        SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        SetEnvironmentVariable("SeedAdmin__Email", AdminEmail);
        SetEnvironmentVariable("SeedAdmin__Password", AdminPassword);
        SetEnvironmentVariable("SeedAdmin__Name", "Admin");
        SetEnvironmentVariable("SeedAdmin__Lastname", "Integration");
        SetEnvironmentVariable("Cors__AdminOrigins__0", "http://localhost:5173");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        foreach (var (name, value) in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        await _postgres.DisposeAsync();
        Dispose();
    }

    public async Task EnsureEmployeeAsync()
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = await userManager.FindByEmailAsync(EmployeeEmail);
        if (user is not null)
        {
            return;
        }

        user = new User
        {
            UserName = EmployeeEmail,
            Email = EmployeeEmail,
            EmailConfirmed = true,
            Name = "Empleado",
            Lastname = "Integracion",
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, EmployeePassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Employee);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(error => error.Description)));
        }
    }

    public async Task<SeededProduct> SeedProductAsync(
        int quantity,
        int receivedQuantity,
        int availableQuantity,
        decimal salePrice = 1000m)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var nextProductDetailCode = (await context.ProductDetails.MaxAsync(item => (int?)item.Code) ?? 0) + 1;

        if (!await context.DollarExchangeRates.AnyAsync(rate => rate.Enabled && rate.StartDate <= DateTime.UtcNow))
        {
            context.DollarExchangeRates.Add(new DollarExchangeRate
            {
                StartDate = DateTime.UtcNow.Date.AddDays(-1),
                StoreRate = 36m,
                BankRate = 36m,
                Enabled = true
            });
        }

        var category = new Category { Name = $"Categoría integración {suffix}" };
        var subcategory = new Subcategory { Name = $"Subcategoría integración {suffix}", Category = category };
        var sizeGroup = new SizeGroup { Name = $"Tallas integración {suffix}" };
        var size = new Size { Name = "M", SizeGroup = sizeGroup, DisplayOrder = 1 };
        var supplier = new Supplier { Name = $"Proveedor integración {suffix}" };
        var order = new Order
        {
            Supplier = supplier,
            PurchaseDate = DateTime.UtcNow,
            OrderStatusId = (int)OrderStatusCode.Pending,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            ExchangeRate = 36m,
            MerchandiseTotalNio = quantity * 400m,
            TotalCostNio = quantity * 400m
        };
        var detail = new ProductDetail
        {
            SupplierProductCode = $"SKU-{suffix}",
            Code = nextProductDetailCode,
            Name = $"Producto integración {suffix}",
            Subcategory = subcategory
        };
        var product = new Product
        {
            Order = order,
            ProductDetail = detail,
            Size = size,
            Quantity = quantity,
            ReceivedQuantity = receivedQuantity,
            AvailableQuantity = availableQuantity,
            UnitCostNio = 400m,
            MerchandiseTotalCostNio = quantity * 400m,
            TotalCostNio = quantity * 400m,
            SalePrice = salePrice
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return new SeededProduct(order.Id, product.Id);
    }

    public async Task<ProductStock> GetProductStockAsync(int productId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = await context.Products.SingleAsync(item => item.Id == productId);

        return new ProductStock(product.ReceivedQuantity, product.AvailableQuantity, product.ReservedQuantity, product.UnavailableQuantity);
    }

    public async Task<DeliveryLocation> SeedDeliveryLocationAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var department = new Department { Name = $"Departamento {suffix}" };
        var municipality = new Municipality { Name = $"Municipio {suffix}", Department = department };
        var agency = new DeliveryAgency
        {
            Name = $"Agencia {suffix}",
            PhoneNumber = "88880000",
            CanCollectCashOnDelivery = true
        };

        context.AddRange(municipality, agency);
        await context.SaveChangesAsync();
        return new DeliveryLocation(municipality.Id, agency.Id);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    private void SetEnvironmentVariable(string name, string value)
    {
        _originalEnvironmentVariables[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public sealed record SeededProduct(int OrderId, int ProductId);
    public sealed record ProductStock(int ReceivedQuantity, int AvailableQuantity, int ReservedQuantity, int UnavailableQuantity);
    public sealed record DeliveryLocation(int MunicipalityId, int DeliveryAgencyId);
}
