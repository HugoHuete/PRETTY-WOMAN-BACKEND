using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Interfaces;

public interface IInventoryService
{
    InventoryMovement Move(
        Product product,
        InventoryStockBucketOption fromStockBucket,
        InventoryStockBucketOption toStockBucket,
        int quantity,
        InventoryMovementTypeOption movementType,
        DateTime movementDate,
        string? comments = null);
}
