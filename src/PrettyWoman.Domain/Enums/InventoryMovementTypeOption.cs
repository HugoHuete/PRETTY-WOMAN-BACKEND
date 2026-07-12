namespace PrettyWoman.Domain.Enums;

public enum InventoryMovementTypeOption
{
    PurchaseReceived = 1,
    Sale = 2,
    SaleCancelled = 3,
    CustomerReturn = 4, // Devuelto
    ExchangeReturn = 5, // Cambiado

    // Movimientos generados por ProductInventoryIssue. El detalle del motivo/resultado vive en el issue.
    IssueOpened = 6,
    IssueReturnedToAvailable = 7,
    IssueRemovedFromInventory = 8,

    // Estas reservaciones sumas y restan a la cantidad "reservada"
    ReservationCreated = 9,
    ReservationReleased = 10,
    ReservationConvertedToSale = 11,

    Donation = 12,
    AdjustmentIncrease = 13,
    AdjustmentDecrease = 14,

    SelectionSent = 15,
    SelectionConvertedToSale = 16,
    SelectionReturned = 17

}
