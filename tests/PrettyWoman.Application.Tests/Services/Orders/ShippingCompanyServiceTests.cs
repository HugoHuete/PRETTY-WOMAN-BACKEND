using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Orders;

public class ShippingCompanyServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsShippingCompaniesOrderedByName()
    {
        await using var context = CreateContext();
        context.ShippingCompanies.AddRange(
            new ShippingCompany { Name = "Zeta Express", Url = "https://zeta.example" },
            new ShippingCompany { Name = "Alfa Cargo", Url = "https://alfa.example" });
        await context.SaveChangesAsync();

        var result = await new ShippingCompanyService(context).GetAllAsync();

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("Alfa Cargo", first.Name);
                Assert.Equal("https://alfa.example", first.Url);
            },
            second => Assert.Equal("Zeta Express", second.Name));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
