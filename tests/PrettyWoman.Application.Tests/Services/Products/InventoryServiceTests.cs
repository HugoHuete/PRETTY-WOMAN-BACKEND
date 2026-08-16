using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Products;

public class InventoryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new InventoryService(_context);
    }

    [Fact]
    public void Move_AvailableToUnavailable_UpdatesBucketsAndCreatesMovement()
    {
        var productVariant = CreateProduct();
        var movementDate = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);

        var movement = _service.Move(
            productVariant,
            InventoryStockBucketOption.Available,
            InventoryStockBucketOption.Unavailable,
            2,
            InventoryMovementTypeOption.IssueOpened,
            movementDate,
            "Costura abierta");

        Assert.Equal(3, productVariant.AvailableQuantity);
        Assert.Equal(2, productVariant.UnavailableQuantity);
        Assert.Equal(5, productVariant.ReceivedQuantity);
        Assert.Same(productVariant, movement.ProductVariant);
        Assert.Equal((int)InventoryStockBucketOption.Available, movement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Unavailable, movement.ToStockBucketId);
        Assert.Equal((int)InventoryMovementTypeOption.IssueOpened, movement.InventoryMovementTypeId);
        Assert.Equal(2, movement.Quantity);
        Assert.Equal(movementDate, movement.MovementDate);
        Assert.Equal("Costura abierta", movement.Comments);
        Assert.Contains(movement, _context.InventoryMovements.Local);
    }

    [Fact]
    public void Move_ExternalToAvailable_IncreasesReceivedAndAvailable()
    {
        var productVariant = CreateProduct();
        productVariant.Quantity = 7;

        _service.Move(
            productVariant,
            InventoryStockBucketOption.External,
            InventoryStockBucketOption.Available,
            2,
            InventoryMovementTypeOption.PurchaseReceived,
            DateTime.UtcNow);

        Assert.Equal(7, productVariant.ReceivedQuantity);
        Assert.Equal(7, productVariant.AvailableQuantity);
    }

    [Fact]
    public void Move_ExternalToAvailable_AllowsReceivedQuantityAbovePurchasedQuantity()
    {
        var productVariant = CreateProduct();

        _service.Move(
            productVariant,
            InventoryStockBucketOption.External,
            InventoryStockBucketOption.Available,
            1,
            InventoryMovementTypeOption.PurchaseReceived,
            DateTime.UtcNow);

        Assert.Equal(6, productVariant.ReceivedQuantity);
        Assert.Equal(6, productVariant.AvailableQuantity);
        Assert.Single(_context.InventoryMovements.Local);
    }

    [Fact]
    public void Move_OutOfInventoryToAvailable_DoesNotIncreaseReceived()
    {
        var productVariant = CreateProduct();
        productVariant.AvailableQuantity = 4;

        _service.Move(
            productVariant,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Available,
            1,
            InventoryMovementTypeOption.SaleCancelled,
            DateTime.UtcNow);

        Assert.Equal(5, productVariant.ReceivedQuantity);
        Assert.Equal(5, productVariant.AvailableQuantity);
    }

    [Fact]
    public void Move_OutOfInventoryToUnavailable_AddsDamagedReturnWithoutIncreasingReceived()
    {
        var productVariant = CreateProduct();
        productVariant.AvailableQuantity = 4;

        var movement = _service.Move(
            productVariant,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Unavailable,
            1,
            InventoryMovementTypeOption.CustomerReturn,
            DateTime.UtcNow,
            "Devolución recibida dañada.");

        Assert.Equal(5, productVariant.ReceivedQuantity);
        Assert.Equal(4, productVariant.AvailableQuantity);
        Assert.Equal(1, productVariant.UnavailableQuantity);
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, movement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Unavailable, movement.ToStockBucketId);
        Assert.Contains(movement, _context.InventoryMovements.Local);
    }

    [Fact]
    public void Move_RejectsInsufficientSourceStockWithoutChangingProduct()
    {
        var productVariant = CreateProduct();

        var exception = Assert.Throws<AppBadRequestException>(() => _service.Move(
            productVariant,
            InventoryStockBucketOption.Available,
            InventoryStockBucketOption.OutOfInventory,
            6,
            InventoryMovementTypeOption.Sale,
            DateTime.UtcNow));

        Assert.Equal("La variante con id '1' no tiene suficiente inventario disponible.", exception.Message);
        Assert.Equal(5, productVariant.ReceivedQuantity);
        Assert.Equal(5, productVariant.AvailableQuantity);
        Assert.Equal(0, productVariant.ReservedQuantity);
        Assert.Equal(0, productVariant.UnavailableQuantity);
    }

    [Fact]
    public void Move_RejectsUnsupportedTransitionWithoutChangingProduct()
    {
        var productVariant = CreateProduct();
        productVariant.AvailableQuantity = 4;
        productVariant.ReservedQuantity = 1;

        var exception = Assert.Throws<AppBadRequestException>(() => _service.Move(
            productVariant,
            InventoryStockBucketOption.Reserved,
            InventoryStockBucketOption.Unavailable,
            1,
            InventoryMovementTypeOption.AdjustmentTransfer,
            DateTime.UtcNow));

        Assert.Equal("La transición de inventario 'Reserved -> Unavailable' no está permitida.", exception.Message);
        Assert.Equal(4, productVariant.AvailableQuantity);
        Assert.Equal(1, productVariant.ReservedQuantity);
        Assert.Equal(0, productVariant.UnavailableQuantity);
    }

    [Fact]
    public void Move_AllowsOutOfInventoryToReturnToReservedAfterFailedDelivery()
    {
        var productVariant = CreateProduct();
        productVariant.AvailableQuantity = 3;

        var movement = _service.Move(
            productVariant,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Reserved,
            2,
            InventoryMovementTypeOption.ReservationCreated,
            DateTime.UtcNow);

        Assert.Equal(3, productVariant.AvailableQuantity);
        Assert.Equal(2, productVariant.ReservedQuantity);
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, movement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Reserved, movement.ToStockBucketId);
    }

    [Fact]
    public void Move_RejectsWhenResultWouldExceedReceivedQuantity()
    {
        var productVariant = CreateProduct();

        var exception = Assert.Throws<AppBadRequestException>(() => _service.Move(
            productVariant,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Available,
            1,
            InventoryMovementTypeOption.AdjustmentTransfer,
            DateTime.UtcNow));

        Assert.Equal("La transición dejaría más inventario activo que recibido en la variante con id '1'.", exception.Message);
        Assert.Equal(5, productVariant.AvailableQuantity);
    }

    private static ProductVariant CreateProduct()
    {
        return new ProductVariant
        {
            Id = 1,
            Quantity = 5,
            ReceivedQuantity = 5,
            AvailableQuantity = 5,
            ReservedQuantity = 0,
            UnavailableQuantity = 0
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
