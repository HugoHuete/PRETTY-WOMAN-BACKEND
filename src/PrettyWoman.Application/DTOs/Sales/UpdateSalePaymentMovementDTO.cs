using System.Text.Json.Serialization;

namespace PrettyWoman.Application.DTOs.Sales;

public class UpdateSalePaymentMovementDTO
{
    private DateTime? _movementDate;
    private int? _paymentMethodId;
    private int? _paymentTerminalId;
    private decimal? _grossAmount;

    public DateTime? MovementDate { get => _movementDate; set { _movementDate = value; HasMovementDate = true; } }
    public int? PaymentMethodId { get => _paymentMethodId; set { _paymentMethodId = value; HasPaymentMethodId = true; } }
    public int? PaymentTerminalId { get => _paymentTerminalId; set { _paymentTerminalId = value; HasPaymentTerminalId = true; } }
    public decimal? GrossAmount { get => _grossAmount; set { _grossAmount = value; HasGrossAmount = true; } }

    [JsonIgnore] public bool HasMovementDate { get; private set; }
    [JsonIgnore] public bool HasPaymentMethodId { get; private set; }
    [JsonIgnore] public bool HasPaymentTerminalId { get; private set; }
    [JsonIgnore] public bool HasGrossAmount { get; private set; }

    [JsonIgnore]
    public bool HasChanges => HasMovementDate || HasPaymentMethodId || HasPaymentTerminalId || HasGrossAmount;
}
