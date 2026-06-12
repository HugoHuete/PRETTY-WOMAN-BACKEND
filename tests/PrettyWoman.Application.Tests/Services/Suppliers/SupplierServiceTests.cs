using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PrettyWoman.Application.DTOs.Suppliers;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Mappings;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Suppliers;

public class SupplierServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(config =>
    {
        config.AddProfile<CatalogProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task CreateAsync_CreatesSupplierWithTrimmedName()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var supplierId = await service.CreateAsync(new CreateSupplierDTO
        {
            Name = "  Zara  ",
            Url = "https://example.com/zara"
        });

        var supplier = await context.Suppliers.SingleAsync();

        Assert.Equal(supplier.Id, supplierId);
        Assert.Equal("Zara", supplier.Name);
        Assert.Equal("https://example.com/zara", supplier.Url);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenSupplierNameAlreadyExists()
    {
        await using var context = CreateContext();
        context.Suppliers.Add(new Supplier { Name = "Zara", Url = "https://example.com/zara" });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateSupplierDTO
        {
            Name = " zara ",
            Url = "https://example.com/other"
        }));

        Assert.Equal("Ya existe un proveedor con ese nombre.", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSupplier()
    {
        await using var context = CreateContext();
        var supplier = new Supplier { Name = "Mango", Url = "https://example.com/mango" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetByIdAsync(supplier.Id);

        Assert.Equal("Mango", result.Name);
        Assert.Equal("https://example.com/mango", result.Url);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsWhenSupplierDoesNotExist()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.GetByIdAsync(999));

        Assert.Equal("El proveedor con id '999' no existe.", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSuppliers()
    {
        await using var context = CreateContext();
        context.Suppliers.AddRange(
            new Supplier { Name = "Zara", Url = "https://example.com/zara" },
            new Supplier { Name = "Mango", Url = "https://example.com/mango" });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, supplier => supplier.Name == "Zara");
        Assert.Contains(result, supplier => supplier.Name == "Mango");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSupplierDataAndTrimsName()
    {
        await using var context = CreateContext();
        var supplier = new Supplier { Name = "Zara", Url = "https://example.com/zara" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.UpdateAsync(supplier.Id, new UpdateSupplierDTO
        {
            Name = "  Zara Updated  ",
            Url = "https://example.com/updated"
        });

        var updatedSupplier = await context.Suppliers.SingleAsync();

        Assert.Equal("Zara Updated", updatedSupplier.Name);
        Assert.Equal("https://example.com/updated", updatedSupplier.Url);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenUpdatedNameAlreadyExists()
    {
        await using var context = CreateContext();
        context.Suppliers.AddRange(
            new Supplier { Name = "Zara", Url = "https://example.com/zara" },
            new Supplier { Name = "Mango", Url = "https://example.com/mango" });
        await context.SaveChangesAsync();

        var supplierToUpdate = await context.Suppliers.SingleAsync(s => s.Name == "Mango");
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.UpdateAsync(supplierToUpdate.Id, new UpdateSupplierDTO
        {
            Name = " zara ",
            Url = "https://example.com/mango-updated"
        }));

        Assert.Equal("Ya existe un proveedor con ese nombre.", exception.Message);
    }

    private static SupplierService CreateService(ApplicationDbContext context)
    {
        return new SupplierService(context, Mapper);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
