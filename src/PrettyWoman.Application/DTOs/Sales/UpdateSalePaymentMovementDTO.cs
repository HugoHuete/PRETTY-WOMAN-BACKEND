using System.Text.Json.Serialization;

namespace PrettyWoman.Application.DTOs.Sales;

public class UpdateSalePaymentMovementDTO
{
    private DateTime? _movementDate;
    private int? _paymentMethodId;
    private int? _paymentTerminalId;
    private decimal? _productAmount;
    private decimal? _shippingAmount;
    private int? _saleDeliveryId;

    public DateTime? MovementDate { get => _movementDate; set { _movementDate = value; HasMovementDate = true; } }
    public int? PaymentMethodId { get => _paymentMethodId; set { _paymentMethodId = value; HasPaymentMethodId = true; } }
    public int? PaymentTerminalId { get => _paymentTerminalId; set { _paymentTerminalId = value; HasPaymentTerminalId = true; } }
    public decimal? ProductAmount { get => _productAmount; set { _productAmount = value; HasProductAmount = true; } }
    public decimal? ShippingAmount { get => _shippingAmount; set { _shippingAmount = value; HasShippingAmount = true; } }
    public int? SaleDeliveryId { get => _saleDeliveryId; set { _saleDeliveryId = value; HasSaleDeliveryId = true; } }

    [JsonIgnore] public bool HasMovementDate { get; private set; }
    [JsonIgnore] public bool HasPaymentMethodId { get; private set; }
    [JsonIgnore] public bool HasPaymentTerminalId { get; private set; }
    [JsonIgnore] public bool HasProductAmount { get; private set; }
    [JsonIgnore] public bool HasShippingAmount { get; private set; }
    [JsonIgnore] public bool HasSaleDeliveryId { get; private set; }

    [JsonIgnore]
    public bool HasAllocationChanges => HasProductAmount || HasShippingAmount || HasSaleDeliveryId;

    [JsonIgnore]
    public bool HasChanges => HasMovementDate || HasPaymentMethodId || HasPaymentTerminalId || HasAllocationChanges;
}