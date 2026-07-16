using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class InventoryService(IApplicationDbContext context) : IInventoryService
{
    private readonly IApplicationDbContext _context = context;

    private static readonly HashSet<(InventoryStockBucketOption From, InventoryStockBucketOption To)> AllowedTransitions =
    [
        (InventoryStockBucketOption.External, InventoryStockBucketOption.Available),
        (InventoryStockBucketOption.Available, InventoryStockBucketOption.Reserved),
        (InventoryStockBucketOption.Available, InventoryStockBucketOption.Unavailable),
        (InventoryStockBucketOption.Available, InventoryStockBucketOption.OutOfInventory),
        (InventoryStockBucketOption.Reserved, InventoryStockBucketOption.Available),
        (InventoryStockBucketOption.Reserved, InventoryStockBucketOption.OutOfInventory),
        (InventoryStockBucketOption.Unavailable, InventoryStockBucketOption.Available),
        (InventoryStockBucketOption.Unavailable, InventoryStockBucketOption.OutOfInventory),
        (InventoryStockBucketOption.OutOfInventory, InventoryStockBucketOption.Available),
        (InventoryStockBucketOption.OutOfInventory, InventoryStockBucketOption.Unavailable)
    ];

    public InventoryMovement Move(
        Product product,
        InventoryStockBucketOption fromStockBucket,
        InventoryStockBucketOption toStockBucket,
        int quantity,
        InventoryMovementTypeOption movementType,
        DateTime movementDate,
        string? comments = null)
    {
        ValidateRequest(fromStockBucket, toStockBucket, quantity, movementType);

        var projected = new ProjectedInventory(
            product.ReceivedQuantity,
            product.AvailableQuantity,
            product.ReservedQuantity,
            product.UnavailableQuantity);

        projected = RemoveFromSource(product.Id, projected, fromStockBucket, quantity);
        projected = AddToDestination(projected, toStockBucket, quantity);
        ValidateProjectedInventory(product, projected);

        var movement = new InventoryMovement
        {
            Product = product,
            InventoryMovementTypeId = (int)movementType,
            FromStockBucketId = (int)fromStockBucket,
            ToStockBucketId = (int)toStockBucket,
            Quantity = quantity,
            MovementDate = movementDate,
            Comments = comments
        };

        _context.InventoryMovements.Add(movement);

        product.ReceivedQuantity = projected.ReceivedQuantity;
        product.AvailableQuantity = projected.AvailableQuantity;
        product.ReservedQuantity = projected.ReservedQuantity;
        product.UnavailableQuantity = projected.UnavailableQuantity;

        return movement;
    }

    private static void ValidateRequest(
        InventoryStockBucketOption fromStockBucket,
        InventoryStockBucketOption toStockBucket,
        int quantity,
        InventoryMovementTypeOption movementType)
    {
        if (quantity <= 0)
        {
            throw new AppBadRequestException("La cantidad del movimiento debe ser mayor que cero.");
        }

        if (!Enum.IsDefined(fromStockBucket) || !Enum.IsDefined(toStockBucket))
        {
            throw new AppBadRequestException("El bucket de inventario no es válido.");
        }

        if (!Enum.IsDefined(movementType))
        {
            throw new AppBadRequestException("El tipo de movimiento de inventario no es válido.");
        }

        if (!AllowedTransitions.Contains((fromStockBucket, toStockBucket)))
        {
            throw new AppBadRequestException(
                $"La transición de inventario '{fromStockBucket} -> {toStockBucket}' no está permitida.");
        }
    }

    private static ProjectedInventory RemoveFromSource(
        int productId,
        ProjectedInventory inventory,
        InventoryStockBucketOption source,
        int quantity)
    {
        return source switch
        {
            // For example, purchases add to the received quantity, but we don't subtract from it when moving to another bucket.
            InventoryStockBucketOption.External => inventory with
            {
                ReceivedQuantity = inventory.ReceivedQuantity + quantity
            },
            InventoryStockBucketOption.Available => inventory with
            {
                AvailableQuantity = SubtractAvailable(productId, inventory.AvailableQuantity, quantity)
            },
            InventoryStockBucketOption.Reserved => inventory with
            {
                ReservedQuantity = SubtractReserved(productId, inventory.ReservedQuantity, quantity)
            },
            InventoryStockBucketOption.Unavailable => inventory with
            {
                UnavailableQuantity = SubtractUnavailable(productId, inventory.UnavailableQuantity, quantity)
            },
            InventoryStockBucketOption.OutOfInventory => inventory,

            _ => throw new AppBadRequestException("El bucket origen de inventario no es válido.")
        };
    }

    private static ProjectedInventory AddToDestination(
        ProjectedInventory inventory,
        InventoryStockBucketOption destination,
        int quantity)
    {
        return destination switch
        {
            InventoryStockBucketOption.Available => inventory with
            {
                AvailableQuantity = inventory.AvailableQuantity + quantity
            },
            InventoryStockBucketOption.Reserved => inventory with
            {
                ReservedQuantity = inventory.ReservedQuantity + quantity
            },
            InventoryStockBucketOption.Unavailable => inventory with
            {
                UnavailableQuantity = inventory.UnavailableQuantity + quantity
            },
            InventoryStockBucketOption.OutOfInventory => inventory,

            _ => throw new AppBadRequestException("El bucket destino de inventario no es válido.")
        };
    }

    private static int SubtractAvailable(int productId, int currentQuantity, int quantity)
    {
        if (currentQuantity < quantity)
        {
            throw new AppBadRequestException($"La variante con id '{productId}' no tiene suficiente inventario disponible.");
        }

        return currentQuantity - quantity;
    }

    private static int SubtractReserved(int productId, int currentQuantity, int quantity)
    {
        if (currentQuantity < quantity)
        {
            throw new AppBadRequestException($"La variante con id '{productId}' no tiene suficiente inventario reservado.");
        }

        return currentQuantity - quantity;
    }

    private static int SubtractUnavailable(int productId, int currentQuantity, int quantity)
    {
        if (currentQuantity < quantity)
        {
            throw new AppBadRequestException($"La variante con id '{productId}' no tiene suficiente inventario no disponible.");
        }

        return currentQuantity - quantity;
    }

    private static void ValidateProjectedInventory(Product product, ProjectedInventory inventory)
    {
        if (inventory.ReceivedQuantity < 0 ||
            inventory.AvailableQuantity < 0 ||
            inventory.ReservedQuantity < 0 ||
            inventory.UnavailableQuantity < 0)
        {
            throw new AppBadRequestException($"La transición dejaría cantidades negativas en la variante con id '{product.Id}'.");
        }

        if (inventory.ReceivedQuantity > product.Quantity)
        {
            throw new AppBadRequestException(
                $"La transición dejaría más inventario recibido que comprado en la variante con id '{product.Id}'.");
        }

        var activeQuantity =
            inventory.AvailableQuantity +
            inventory.ReservedQuantity +
            inventory.UnavailableQuantity;

        if (activeQuantity > inventory.ReceivedQuantity)
        {
            throw new AppBadRequestException(
                $"La transición dejaría más inventario activo que recibido en la variante con id '{product.Id}'.");
        }
    }

    private sealed record ProjectedInventory(
        int ReceivedQuantity,
        int AvailableQuantity,
        int ReservedQuantity,
        int UnavailableQuantity);
}
