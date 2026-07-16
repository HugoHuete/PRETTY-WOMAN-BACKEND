using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Sales;

public class SaleReturnServiceTests
{
    [Fact]
    public async Task ReceiveAsync_ReturnsSellableItemToAvailableInventory()
    {
        await using var context = CreateContext();
        var (saleReturn, item, product) = await SeedRequestedInStoreReturnAsync(context);
        var service = CreateService(context);
        var receivedAt = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);

        await service.ReceiveAsync(saleReturn.OriginalSaleId, saleReturn.Id, new ReceiveSaleReturnDTO
        {
            ReceivedAt = receivedAt,
            Items =
            [
                new ReceiveSaleReturnItemDTO
                {
                    SaleReturnItemId = item.Id,
                    IsDamaged = false,
                    Comments = "Prenda en buen estado"
                }
            ]
        });

        product = await context.Products.SingleAsync(storedProduct => storedProduct.Id == product.Id);
        item = await context.SaleReturnItems.SingleAsync(storedItem => storedItem.Id == item.Id);
        saleReturn = await context.SaleReturns.SingleAsync(storedReturn => storedReturn.Id == saleReturn.Id);
        var movement = await context.InventoryMovements.SingleAsync();

        Assert.Equal(1, product.ReceivedQuantity);
        Assert.Equal(1, product.AvailableQuantity);
        Assert.Equal(0, product.UnavailableQuantity);
        Assert.Equal(receivedAt, item.ReceivedAt);
        Assert.Equal("Prenda en buen estado", item.Comments);
        Assert.Null(item.ProductInventoryIssueId);
        Assert.Equal((int)SaleReturnStatusOption.Completed, saleReturn.StatusId);
        Assert.Equal((int)InventoryMovementTypeOption.CustomerReturn, movement.InventoryMovementTypeId);
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, movement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Available, movement.ToStockBucketId);
        Assert.Equal(item.Id, movement.SaleReturnItemId);
        Assert.Null(movement.ProductInventoryIssueId);
    }

    [Fact]
    public async Task ReceiveAsync_MovesDamagedItemToUnavailableAndCreatesIssue()
    {
        await using var context = CreateContext();
        var (saleReturn, item, product) = await SeedRequestedInStoreReturnAsync(context);
        var service = CreateService(context);
        var receivedAt = new DateTime(2026, 7, 17, 11, 0, 0, DateTimeKind.Utc);

        await service.ReceiveAsync(saleReturn.OriginalSaleId, saleReturn.Id, new ReceiveSaleReturnDTO
        {
            ReceivedAt = receivedAt,
            Items =
            [
                new ReceiveSaleReturnItemDTO
                {
                    SaleReturnItemId = item.Id,
                    IsDamaged = true,
                    Comments = "Cierre dañado"
                }
            ]
        });

        product = await context.Products.SingleAsync(storedProduct => storedProduct.Id == product.Id);
        item = await context.SaleReturnItems.SingleAsync(storedItem => storedItem.Id == item.Id);
        var issue = await context.ProductInventoryIssues.SingleAsync();
        var movement = await context.InventoryMovements.SingleAsync();

        Assert.Equal(1, product.ReceivedQuantity);
        Assert.Equal(0, product.AvailableQuantity);
        Assert.Equal(1, product.UnavailableQuantity);
        Assert.Equal(issue.Id, item.ProductInventoryIssueId);
        Assert.Equal((int)ProductInventoryIssueTypeOption.Damaged, issue.ProductInventoryIssueTypeId);
        Assert.Equal((int)ProductInventoryIssueStatusOption.Open, issue.ProductInventoryIssueStatusId);
        Assert.Equal(1, issue.Quantity);
        Assert.Equal(receivedAt, issue.IssueDate);
        Assert.Equal("Cierre dañado", issue.Comments);
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, movement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Unavailable, movement.ToStockBucketId);
        Assert.Equal(item.Id, movement.SaleReturnItemId);
        Assert.Equal(issue.Id, movement.ProductInventoryIssueId);
    }

    private static SaleReturnService CreateService(ApplicationDbContext context)
    {
        return new SaleReturnService(context, new InventoryService(context));
    }

    private static async Task<(SaleReturn SaleReturn, SaleReturnItem Item, Product Product)> SeedRequestedInStoreReturnAsync(
        ApplicationDbContext context)
    {
        var product = new Product
        {
            Id = 1,
            OrderId = 1,
            ProductDetailId = 1,
            SizeId = 1,
            Quantity = 1,
            ReceivedQuantity = 1,
            AvailableQuantity = 0,
            ReservedQuantity = 0,
            UnavailableQuantity = 0
        };
        var saleReturn = new SaleReturn
        {
            Id = 1,
            OriginalSaleId = 10,
            ReasonId = (int)SaleReturnReasonOption.CustomerPreference,
            MethodId = (int)SaleReturnMethodOption.InStore,
            StatusId = (int)SaleReturnStatusOption.Requested,
            RefundTotal = 0
        };
        var item = new SaleReturnItem
        {
            Id = 1,
            SaleReturn = saleReturn,
            OriginalSaleProductId = 1,
            Product = product,
            Quantity = 1
        };
        saleReturn.Items.Add(item);

        context.Products.Add(product);
        context.SaleReturns.Add(saleReturn);
        await context.SaveChangesAsync();

        return (saleReturn, item, product);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
