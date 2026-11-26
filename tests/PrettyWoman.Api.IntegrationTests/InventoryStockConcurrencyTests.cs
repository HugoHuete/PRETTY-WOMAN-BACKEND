using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class InventoryStockConcurrencyTests(PrettyWomanApiFactory factory)
{
    [Fact]
    public async Task StaleExchangeReservationCannotClaimUnitUsedByDeliveryCorrection()
    {
        var product = await factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1);
        await using var reservationScope = factory.Services.CreateAsyncScope();
        await using var correctionScope = factory.Services.CreateAsyncScope();
        var reservation = reservationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var correction = correctionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var stale = await reservation.ProductVariants.SingleAsync(variant => variant.Id == product.ProductVariantId);
        var actual = await correction.ProductVariants.SingleAsync(variant => variant.Id == product.ProductVariantId);
        new InventoryService(reservation).Move(stale, InventoryStockBucketOption.Available,
            InventoryStockBucketOption.Reserved, 1, InventoryMovementTypeOption.ExchangeReplacementReserved, DateTime.UtcNow);
        new InventoryService(correction).Move(actual, InventoryStockBucketOption.Available,
            InventoryStockBucketOption.OutOfInventory, 1, InventoryMovementTypeOption.AdjustmentTransfer, DateTime.UtcNow);
        await correction.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => reservation.SaveChangesAsync());

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stock = await verification.ProductVariants.SingleAsync(variant => variant.Id == product.ProductVariantId);
        Assert.Equal(0, stock.AvailableQuantity);
        Assert.Equal(0, stock.ReservedQuantity);
        var movement = Assert.Single(await verification.InventoryMovements.Where(row => row.ProductId == stock.Id).ToListAsync());
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, movement.ToStockBucketId);
    }
}
