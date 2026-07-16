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
        var product = CreateProduct();
        var movementDate = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);

        var movement = _service.Move(
            product,
            InventoryStockBucketOption.Available,
            InventoryStockBucketOption.Unavailable,
            2,
            InventoryMovementTypeOption.IssueOpened,
            movementDate,
            "Costura abierta");

        Assert.Equal(3, product.AvailableQuantity);
        Assert.Equal(2, product.UnavailableQuantity);
        Assert.Equal(5, product.ReceivedQuantity);
        Assert.Same(product, movement.Product);
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
        var product = CreateProduct();
        product.Quantity = 7;

        _service.Move(
            product,
            InventoryStockBucketOption.External,
            InventoryStockBucketOption.Available,
            2,
            InventoryMovementTypeOption.PurchaseReceived,
            DateTime.UtcNow);

        Assert.Equal(7, product.ReceivedQuantity);
        Assert.Equal(7, product.AvailableQuantity);
    }

    [Fact]
    public void Move_ExternalToAvailable_RejectsReceivedQuantityAbovePurchasedQuantity()
    {
        var product = CreateProduct();

        var exception = Assert.Throws<AppBadRequestException>(() => _service.Move(
            product,
            InventoryStockBucketOption.External,
            InventoryStockBucketOption.Available,
            1,
            InventoryMovementTypeOption.PurchaseReceived,
            DateTime.UtcNow));

        Assert.Equal("La transición dejaría más inventario recibido que comprado en la variante con id '1'.", exception.Message);
        Assert.Equal(5, product.ReceivedQuantity);
        Assert.Equal(5, product.AvailableQuantity);
        Assert.Empty(_context.InventoryMovements.Local);
    }

    [Fact]
    public void Move_OutOfInventoryToAvailable_DoesNotIncreaseReceived()
    {
        var product = CreateProduct();
        product.AvailableQuantity = 4;

        _service.Move(
            product,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Available,
            1,
            InventoryMovementTypeOption.SaleCancelled,
            DateTime.UtcNow);

        Assert.Equal(5, product.ReceivedQuantity);
        Assert.Equal(5, product.AvailableQuantity);
    }

    [Fact]
    public void Move_OutOfInventoryToUnavailable_AddsDamagedReturnWithoutIncreasingReceived()
    {
        var product = CreateProduct();
        product.AvailableQuantity = 4;

        var movement = _service.Move(
            product,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Unavailable,
            1,
            InventoryMovementTypeOption.CustomerReturn,
            DateTime.UtcNow,
            "Devolución recibida dañada.");

        Assert.Equal(5, product.ReceivedQuantity);
        Assert.Equal(4, product.AvailableQuantity);
        Assert.Equal(1, product.UnavailableQuantity);
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, movement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Unavailable, movement.ToStockBucketId);
        Assert.Contains(movement, _context.InventoryMovements.Local);
    }

    [Fact]
    public void Move_RejectsInsufficientSourceStockWithoutChangingProduct()
    {
        var product = CreateProduct();

        var exception = Assert.Throws<AppBadRequestException>(() => _service.Move(
            product,
            InventoryStockBucketOption.Available,
            InventoryStockBucketOption.OutOfInventory,
            6,
            InventoryMovementTypeOption.Sale,
            DateTime.UtcNow));

        Assert.Equal("La variante con id '1' no tiene suficiente inventario disponible.", exception.Message);
        Assert.Equal(5, product.ReceivedQuantity);
        Assert.Equal(5, product.AvailableQuantity);
        Assert.Equal(0, product.ReservedQuantity);
        Assert.Equal(0, product.UnavailableQuantity);
    }

    [Fact]
    public void Move_RejectsUnsupportedTransitionWithoutChangingProduct()
    {
        var product = CreateProduct();
        product.AvailableQuantity = 4;
        product.ReservedQuantity = 1;

        var exception = Assert.Throws<AppBadRequestException>(() => _service.Move(
            product,
            InventoryStockBucketOption.Reserved,
            InventoryStockBucketOption.Unavailable,
            1,
            InventoryMovementTypeOption.AdjustmentDecrease,
            DateTime.UtcNow));

        Assert.Equal("La transición de inventario 'Reserved -> Unavailable' no está permitida.", exception.Message);
        Assert.Equal(4, product.AvailableQuantity);
        Assert.Equal(1, product.ReservedQuantity);
        Assert.Equal(0, product.UnavailableQuantity);
    }

    [Fact]
    public void Move_RejectsWhenResultWouldExceedReceivedQuantity()
    {
        var product = CreateProduct();

        var exception = Assert.Throws<AppBadRequestException>(() => _service.Move(
            product,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Available,
            1,
            InventoryMovementTypeOption.AdjustmentIncrease,
            DateTime.UtcNow));

        Assert.Equal("La transición dejaría más inventario activo que recibido en la variante con id '1'.", exception.Message);
        Assert.Equal(5, product.AvailableQuantity);
    }

    private static Product CreateProduct()
    {
        return new Product
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
