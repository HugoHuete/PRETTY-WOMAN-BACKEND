namespace PrettyWoman.Domain.Enums;

public enum InventoryMovementTypeOption
{
    PurchaseReceived = 1,
    Sale = 2,
    SaleCancelled = 3,
    CustomerReturn = 4, // Devuelto

    // Movimientos generados por ProductInventoryIssue. El detalle del motivo/resultado vive en el issue.
    IssueOpened = 5,
    IssueReturnedToAvailable = 6,
    IssueRemovedFromInventory = 7,

    // Estas reservaciones sumas y restan a la cantidad "reservada"
    ReservationCreated = 8,
    ReservationReleased = 9,
    ReservationConvertedToSale = 10,

    SelectionSent = 11,
    SelectionConvertedToSale = 12,
    SelectionReturned = 13,
    ExchangeReplacementReserved = 14,
    ExchangeReplacementDelivered = 15,
    ExchangeReplacementReservationReleased = 16,
    ExchangeReturnReceivedByAgency = 17,
    ExchangeReturnMissing = 18,
    AdjustmentTransfer = 19

}
