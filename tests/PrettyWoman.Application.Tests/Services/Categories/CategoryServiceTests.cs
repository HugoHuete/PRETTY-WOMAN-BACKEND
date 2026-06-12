using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PrettyWoman.Application.DTOs.Categories;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Mappings;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Categories;

public class CategoryServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(config =>
    {
        config.AddProfile<CatalogProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task CreateAsync_CreatesCategoryWithTrimmedName()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var categoryId = await service.CreateAsync(new CreateCategoryDTO
        {
            Name = "  Blusas  "
        });

        var category = await context.Categories.SingleAsync();

        Assert.Equal(category.Id, categoryId);
        Assert.Equal("Blusas", category.Name);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenCategoryNameAlreadyExists()
    {
        await using var context = CreateContext();
        context.Categories.Add(new Category { Name = "Blusas" });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateCategoryDTO
        {
            Name = " blusas "
        }));

        Assert.Equal("Ya existe una categoría con ese nombre.", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCategory()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Vestidos" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetByIdAsync(category.Id);

        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Vestidos", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsWhenCategoryDoesNotExist()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.GetByIdAsync(999));

        Assert.Equal("La categoría con id '999' no existe.", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        await using var context = CreateContext();
        context.Categories.AddRange(
            new Category { Name = "Blusas" },
            new Category { Name = "Vestidos" });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, category => category.Name == "Blusas");
        Assert.Contains(result, category => category.Name == "Vestidos");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCategoryAndTrimsName()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Blusas" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.UpdateAsync(category.Id, new UpdateCategoryDTO
        {
            Name = "  Blusas Premium  "
        });

        var updatedCategory = await context.Categories.SingleAsync();

        Assert.Equal("Blusas Premium", updatedCategory.Name);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenUpdatedNameAlreadyExists()
    {
        await using var context = CreateContext();
        context.Categories.AddRange(
            new Category { Name = "Blusas" },
            new Category { Name = "Vestidos" });
        await context.SaveChangesAsync();

        var categoryToUpdate = await context.Categories.SingleAsync(category => category.Name == "Vestidos");
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.UpdateAsync(categoryToUpdate.Id, new UpdateCategoryDTO
        {
            Name = " blusas "
        }));

        Assert.Equal("Ya existe una categoría con ese nombre.", exception.Message);
    }

    private static CategoryService CreateService(ApplicationDbContext context)
    {
        return new CategoryService(context, Mapper);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
