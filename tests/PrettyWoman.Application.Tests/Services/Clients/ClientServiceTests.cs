using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PrettyWoman.Application.DTOs.Clients;
using PrettyWoman.Application.Exceptions;
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
    [InlineData("PhoneNumber", "0001", "Alvaro")]
    [InlineData("PhoneNumber", " 8887-0001 ", "Alvaro")]
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
    public async Task GetAllAsync_MatchesAnySuppliedTextFilterBeforePagination()
    {
        await using var context = CreateContext();
        await SeedClientsAsync(context);
        var service = CreateService(context);
        var query = new ClientQueryDTO
        {
            Name = " ALV ",
            PhoneNumber = "7003",
            InstagramUser = "LOOK",
            MessengerUser = "beatriz",
            PageSize = 2
        };

        var firstPage = await service.GetAllAsync(query);
        query.Page = 2;
        var secondPage = await service.GetAllAsync(query);

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(new[] { "Alvaro", "Beatriz" }, firstPage.Items.Select(client => client.Name));
        Assert.Equal("Carla", Assert.Single(secondPage.Items).Name);
    }

    [Fact]
    public async Task GetAllAsync_IgnoresBlankFiltersWhenMatchingAnyField()
    {
        await using var context = CreateContext();
        await SeedClientsAsync(context);

        var result = await CreateService(context).GetAllAsync(new ClientQueryDTO
        {
            Name = "  ",
            PhoneNumber = "7002",
            InstagramUser = ""
        });

        Assert.Equal("Beatriz", Assert.Single(result.Items).Name);
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

    [Fact]
    public async Task CreateAndUpdateAsync_NormalizePhoneBeforeCheckingDuplicatesAndSaving()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var id = await service.CreateAsync(new CreateClientDTO { Name = "Ana", PhoneNumber = " 8949 4589 " });
        Assert.Equal("89494589", (await service.GetByIdAsync(id)).PhoneNumber);

        var otherId = await service.CreateAsync(new CreateClientDTO { Name = "Bea", PhoneNumber = "8858-1572" });
        await Assert.ThrowsAsync<AppBadRequestException>(() =>
            service.CreateAsync(new CreateClientDTO { Name = "Caro", PhoneNumber = "8949-4589" }));
        await Assert.ThrowsAsync<AppBadRequestException>(() =>
            service.UpdateAsync(otherId, new UpdateClientDTO { Name = "Bea", PhoneNumber = "8949 4589" }));

        await service.UpdateAsync(id, new UpdateClientDTO { Name = "Ana", PhoneNumber = "8949-4589" });
        await service.UpdateAsync(id, new UpdateClientDTO { Name = "Ana", PhoneNumber = " 8765-4321 " });
        Assert.Equal("87654321", (await service.GetByIdAsync(id)).PhoneNumber);
    }

    [Theory]
    [InlineData("8949-458")]
    [InlineData("8949-45890")]
    [InlineData("+505 8949-4589")]
    [InlineData("8949-4589x")]
    [InlineData("--")]
    public async Task CreateAsync_RejectsPhoneThatDoesNotNormalizeToEightDigits(string phone)
    {
        await using var context = CreateContext();
        await Assert.ThrowsAsync<AppBadRequestException>(() =>
            CreateService(context).CreateAsync(new CreateClientDTO { Name = "Ana", PhoneNumber = phone }));
    }

    private static ClientService CreateService(ApplicationDbContext context) => new(context, Mapper);

    private static async Task SeedClientsAsync(ApplicationDbContext context)
    {
        context.Clients.AddRange(
            new Client
            {
                Name = "Alvaro",
                PhoneNumber = "88870001",
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