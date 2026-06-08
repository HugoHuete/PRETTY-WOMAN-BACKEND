namespace PrettyWoman.Domain.Enums;
public enum InventoryMovementTypeOption
{
    PurchaseReceived = 1,
    Sale = 2,
    SaleCancelled = 3,
    CustomerReturn = 4, // Devuelto
    ExchangeReturn = 5, // Cambiado
    Damaged = 6,
    Repaired = 7,
    Lost = 8,
    Found = 9,
    Discarded = 10,
    Donation = 11,
    AdjustmentIncrease = 12,
    AdjustmentDecrease = 13,

    // Estas reservaciones sumas y restan a la cantidad "reservada"
    ReservationCreated = 14,
    ReservationReleased = 15,
    ReservationConvertedToSale = 16
}