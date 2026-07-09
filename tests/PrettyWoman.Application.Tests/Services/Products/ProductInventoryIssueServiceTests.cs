using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Products.InventoryIssues;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Products;

public class ProductInventoryIssueServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesOpenIssueMovesInventoryAndCreatesMovement()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);

        var id = await service.CreateAsync(new CreateProductInventoryIssueDTO
        {
            ProductId = 1,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Damaged,
            Quantity = 2,
            IssueDate = new DateTime(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc),
            Comments = "Costura abierta"
        });

        var issue = await context.ProductInventoryIssues
            .Include(issue => issue.InventoryMovements)
            .SingleAsync(issue => issue.Id == id);
        var product = await context.Products.SingleAsync(product => product.Id == 1);
        var movement = Assert.Single(issue.InventoryMovements);

        Assert.Equal((int)ProductInventoryIssueStatusOption.Open, issue.ProductInventoryIssueStatusId);
        Assert.Equal(3, product.AvailableQuantity);
        Assert.Equal(2, product.UnavailableQuantity);
        Assert.Equal((int)InventoryMovementTypeOption.IssueOpened, movement.InventoryMovementTypeId);
        Assert.Equal((int)InventoryStockBucketOption.Available, movement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Unavailable, movement.ToStockBucketId);
        Assert.Equal(2, movement.Quantity);
        Assert.Equal("Costura abierta", movement.Comments);
    }

    [Fact]
    public async Task CreateAsync_RejectsWhenAvailableQuantityIsInsufficient()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateProductInventoryIssueDTO
        {
            ProductId = 1,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Damaged,
            Quantity = 8
        }));

        Assert.Equal("La variante con id '1' no tiene suficiente inventario disponible.", exception.Message);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsIssueToAvailableAndCreatesMovement()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var id = await service.CreateAsync(new CreateProductInventoryIssueDTO
        {
            ProductId = 1,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Repairing,
            Quantity = 1,
            Comments = "En reparacion"
        });

        var result = await service.ResolveAsync(id, new ResolveProductInventoryIssueDTO
        {
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.ResolvedToAvailable,
            ResolvedAt = new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc),
            Comments = "Reparado"
        });

        var product = await context.Products.SingleAsync(product => product.Id == 1);
        var movements = await context.InventoryMovements
            .Where(movement => movement.ProductInventoryIssueId == id)
            .OrderBy(movement => movement.Id)
            .ToListAsync();

        Assert.Equal((int)ProductInventoryIssueStatusOption.ResolvedToAvailable, result.ProductInventoryIssueStatusId);
        Assert.NotNull(result.ResolvedAt);
        Assert.Equal(5, product.AvailableQuantity);
        Assert.Equal(0, product.UnavailableQuantity);
        Assert.Equal(2, movements.Count);
        Assert.Equal((int)InventoryMovementTypeOption.IssueReturnedToAvailable, movements[1].InventoryMovementTypeId);
        Assert.Equal((int)InventoryStockBucketOption.Unavailable, movements[1].FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Available, movements[1].ToStockBucketId);
    }

    [Fact]
    public async Task ResolveAsync_DiscardsIssueWithoutReturningToAvailable()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var id = await service.CreateAsync(new CreateProductInventoryIssueDTO
        {
            ProductId = 1,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Damaged,
            Quantity = 1
        });

        await service.ResolveAsync(id, new ResolveProductInventoryIssueDTO
        {
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.Discarded,
            Comments = "No recuperable"
        });

        var product = await context.Products.SingleAsync(product => product.Id == 1);
        var closingMovement = await context.InventoryMovements
            .Where(movement => movement.ProductInventoryIssueId == id)
            .OrderByDescending(movement => movement.Id)
            .FirstAsync();

        Assert.Equal(4, product.AvailableQuantity);
        Assert.Equal(0, product.UnavailableQuantity);
        Assert.Equal((int)InventoryMovementTypeOption.IssueRemovedFromInventory, closingMovement.InventoryMovementTypeId);
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, closingMovement.ToStockBucketId);
    }

    [Fact]
    public async Task DeleteAsync_CancelsOpenIssueAndReturnsInventoryToAvailable()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var id = await service.CreateAsync(new CreateProductInventoryIssueDTO
        {
            ProductId = 1,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Missing,
            Quantity = 1
        });

        var result = await service.DeleteAsync(id);

        var product = await context.Products.SingleAsync(product => product.Id == 1);
        var closingMovement = await context.InventoryMovements
            .Where(movement => movement.ProductInventoryIssueId == id)
            .OrderByDescending(movement => movement.Id)
            .FirstAsync();

        Assert.Equal((int)ProductInventoryIssueStatusOption.Cancelled, result.ProductInventoryIssueStatusId);
        Assert.Equal(5, product.AvailableQuantity);
        Assert.Equal(0, product.UnavailableQuantity);
        Assert.Equal((int)InventoryMovementTypeOption.IssueReturnedToAvailable, closingMovement.InventoryMovementTypeId);
        Assert.Equal((int)InventoryStockBucketOption.Available, closingMovement.ToStockBucketId);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByProductDetailAndStatus()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var firstIssueId = await service.CreateAsync(new CreateProductInventoryIssueDTO
        {
            ProductId = 1,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Damaged,
            Quantity = 1
        });
        await service.CreateAsync(new CreateProductInventoryIssueDTO
        {
            ProductId = 2,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Missing,
            Quantity = 1
        });
        await service.ResolveAsync(firstIssueId, new ResolveProductInventoryIssueDTO
        {
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.ResolvedToAvailable
        });

        var result = await service.GetAllAsync(new ProductInventoryIssueQueryDTO
        {
            ProductDetailId = 1,
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.Open
        });

        var issue = Assert.Single(result.Items);
        Assert.Equal(2, issue.ProductId);
        Assert.Equal("M", issue.SizeName);
        Assert.Equal((int)ProductInventoryIssueStatusOption.Open, issue.ProductInventoryIssueStatusId);
    }

    [Fact]
    public async Task ResolveAsync_RejectsClosedIssue()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var id = await service.CreateAsync(new CreateProductInventoryIssueDTO
        {
            ProductId = 1,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Damaged,
            Quantity = 1
        });
        await service.DeleteAsync(id);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.ResolveAsync(id, new ResolveProductInventoryIssueDTO
        {
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.ResolvedToAvailable
        }));

        Assert.Equal("Solo se pueden resolver issues abiertos.", exception.Message);
    }

    private static ProductInventoryIssueService CreateService(ApplicationDbContext context)
    {
        return new ProductInventoryIssueService(context);
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        var sizeGroup = new SizeGroup { Id = 1, Name = "Regular" };
        var smallSize = new Size { Id = 1, Name = "S", SizeGroupId = 1, SizeGroup = sizeGroup, DisplayOrder = 1 };
        var mediumSize = new Size { Id = 2, Name = "M", SizeGroupId = 1, SizeGroup = sizeGroup, DisplayOrder = 2 };
        context.SizeGroups.Add(sizeGroup);
        context.Sizes.AddRange(smallSize, mediumSize);

        var productDetail = new ProductDetail
        {
            Id = 1,
            SupplierProductCode = "BLA-001",
            Code = 1001,
            Name = "Blazer",
            SubcategoryId = 1,
            Products =
            [
                new Product { Id = 1, SizeId = 1, Size = smallSize, Quantity = 5, ReceivedQuantity = 5, AvailableQuantity = 5, SalePrice = 800m },
                new Product { Id = 2, SizeId = 2, Size = mediumSize, Quantity = 2, ReceivedQuantity = 2, AvailableQuantity = 2, SalePrice = 800m }
            ]
        };

        context.ProductDetails.Add(productDetail);
        context.ProductInventoryIssueTypes.AddRange(
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.Damaged, Name = "Damaged" },
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.Dirty, Name = "Dirty" },
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.Missing, Name = "Missing" },
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.UnderReview, Name = "UnderReview" },
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.Repairing, Name = "Repairing" });
        context.ProductInventoryIssueStatuses.AddRange(
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.Open, Name = "Open" },
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.ResolvedToAvailable, Name = "ResolvedToAvailable" },
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.Discarded, Name = "Discarded" },
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.ConfirmedLost, Name = "ConfirmedLost" },
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.Cancelled, Name = "Cancelled" });
        context.InventoryMovementTypes.AddRange(
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.IssueOpened, Name = "IssueOpened" },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.IssueReturnedToAvailable, Name = "IssueReturnedToAvailable" },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.IssueRemovedFromInventory, Name = "IssueRemovedFromInventory" });
        context.InventoryStockBuckets.AddRange(
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Available, Name = "Available" },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Unavailable, Name = "Unavailable" },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.OutOfInventory, Name = "OutOfInventory" });

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

