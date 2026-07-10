using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PrettyWoman.Application.DTOs.Clients;
using PrettyWoman.Application.Mappings;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Clients;

public class ClientServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(config =>
    {
        config.AddProfile<ClientsProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();

    [Theory]
    [InlineData("Name", "lva", "Alvaro")]
    [InlineData("PhoneNumber", "7001", "Alvaro")]
    [InlineData("InstagramUser", "look", "Beatriz")]
    [InlineData("MessengerUser", "carla", "Carla")]
    public async Task GetAllAsync_FiltersPartialMatchesByEachTextField(string property, string value, string expectedName)
    {
        await using var context = CreateContext();
        await SeedClientsAsync(context);
        var query = new ClientQueryDTO();
        typeof(ClientQueryDTO).GetProperty(property)!.SetValue(query, value);

        var result = await CreateService(context).GetAllAsync(query);

        var client = Assert.Single(result.Items);
        Assert.Equal(expectedName, client.Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResultAndMapsMessengerUser()
    {
        await using var context = CreateContext();
        await SeedClientsAsync(context);

        var result = await CreateService(context).GetAllAsync(new ClientQueryDTO
        {
            Page = 2,
            PageSize = 2
        });

        var client = Assert.Single(result.Items);
        Assert.Equal("Carla", client.Name);
        Assert.Equal("carla.pm", client.MessengerUser);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.False(result.HasNextPage);
    }

    private static ClientService CreateService(ApplicationDbContext context) => new(context, Mapper);

    private static async Task SeedClientsAsync(ApplicationDbContext context)
    {
        context.Clients.AddRange(
            new Client
            {
                Name = "Alvaro",
                PhoneNumber = "8887001",
                InstagramUser = "alvaro.style",
                CreatedAt = DateTime.UtcNow
            },
            new Client
            {
                Name = "Beatriz",
                PhoneNumber = "8887002",
                InstagramUser = "bea.look",
                MessengerUser = "beatriz.pm",
                CreatedAt = DateTime.UtcNow
            },
            new Client
            {
                Name = "Carla",
                PhoneNumber = "8887003",
                MessengerUser = "carla.pm",
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}