using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PrettyWoman.Application.DTOs.Subcategories;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Mappings;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Categories;

public class SubcategoryServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(config =>
    {
        config.AddProfile<CatalogProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task CreateAsync_CreatesSubcategoryWithTrimmedName()
    {
        await using var context = CreateContext();
        var category = await AddCategoryAsync(context, "Blusas");
        var service = CreateService(context);

        var subcategoryId = await service.CreateAsync(new CreateSubcategoryDTO
        {
            CategoryId = category.Id,
            Name = "  Manga corta  "
        });

        var subcategory = await context.Subcategories.SingleAsync();

        Assert.Equal(subcategory.Id, subcategoryId);
        Assert.Equal(category.Id, subcategory.CategoryId);
        Assert.Equal("Manga corta", subcategory.Name);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenCategoryDoesNotExist()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.CreateAsync(new CreateSubcategoryDTO
        {
            CategoryId = 999,
            Name = "Manga corta"
        }));

        Assert.Equal("La categoría con id '999' no existe.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenSubcategoryNameAlreadyExists()
    {
        await using var context = CreateContext();
        var category = await AddCategoryAsync(context, "Blusas");
        context.Subcategories.Add(new Subcategory { CategoryId = category.Id, Name = "Manga corta" });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateSubcategoryDTO
        {
            CategoryId = category.Id,
            Name = " manga corta "
        }));

        Assert.Equal("Ya existe una subcategoría con ese nombre.", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSubcategoryWithCategoryName()
    {
        await using var context = CreateContext();
        var category = await AddCategoryAsync(context, "Blusas");
        var subcategory = new Subcategory { CategoryId = category.Id, Name = "Manga corta" };
        context.Subcategories.Add(subcategory);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetByIdAsync(subcategory.Id);

        Assert.Equal(subcategory.Id, result.Id);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal("Manga corta", result.Name);
        Assert.Equal("Blusas", result.CategoryName);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCategory()
    {
        await using var context = CreateContext();
        var blouses = await AddCategoryAsync(context, "Blusas");
        var dresses = await AddCategoryAsync(context, "Vestidos");
        context.Subcategories.AddRange(
            new Subcategory { CategoryId = blouses.Id, Name = "Manga corta" },
            new Subcategory { CategoryId = dresses.Id, Name = "Casual" });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = (await service.GetAllAsync(blouses.Id)).ToList();

        Assert.Single(result);
        Assert.Equal("Manga corta", result[0].Name);
        Assert.Equal("Blusas", result[0].CategoryName);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSubcategoryAndCategory()
    {
        await using var context = CreateContext();
        var blouses = await AddCategoryAsync(context, "Blusas");
        var dresses = await AddCategoryAsync(context, "Vestidos");
        var subcategory = new Subcategory { CategoryId = blouses.Id, Name = "Manga corta" };
        context.Subcategories.Add(subcategory);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.UpdateAsync(subcategory.Id, new UpdateSubcategoryDTO
        {
            CategoryId = dresses.Id,
            Name = "  Casual  "
        });

        var updatedSubcategory = await context.Subcategories.SingleAsync();

        Assert.Equal(dresses.Id, updatedSubcategory.CategoryId);
        Assert.Equal("Casual", updatedSubcategory.Name);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenSubcategoryDoesNotExist()
    {
        await using var context = CreateContext();
        var category = await AddCategoryAsync(context, "Blusas");
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.UpdateAsync(999, new UpdateSubcategoryDTO
        {
            CategoryId = category.Id,
            Name = "Manga corta"
        }));

        Assert.Equal("La subcategoría con id '999' no existe.", exception.Message);
    }

    private static SubcategoryService CreateService(ApplicationDbContext context)
    {
        return new SubcategoryService(context, Mapper);
    }

    private static async Task<Category> AddCategoryAsync(ApplicationDbContext context, string name)
    {
        var category = new Category { Name = name };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return category;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
