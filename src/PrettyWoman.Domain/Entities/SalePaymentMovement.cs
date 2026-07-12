using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

public class SalePaymentMovement : IAuditableEntity
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }
    public int SaleId { get; set; }
    public int MovementDirectionId { get; set; } = (int)MovementDirectionOptions.In;
    public int PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public int? ReversedSalePaymentMovementId { get; set; }
    // GrossAmount = ProductAmount + ShippingAmount
    public decimal GrossAmount { get; set; }
    public decimal ProductAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public int? SaleDeliveryId { get; set; }
    public int? DeliveryAgencyReconciliationId { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal IncomeTaxPercentage { get; set; }
    public decimal IncomeTaxAmount { get; set; }
    public decimal NetReceivedAmount { get; set; }
    public required string UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public Sale? Sale { get; set; }
    public SaleDelivery? SaleDelivery { get; set; }
    public DeliveryAgencyReconciliation? DeliveryAgencyReconciliation { get; set; }
    public MovementDirection? MovementDirection { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentTerminal? PaymentTerminal { get; set; }
    public SalePaymentMovement? ReversedSalePaymentMovement { get; set; }
    public List<SalePaymentMovement> ReversalMovements { get; set; } = [];
}

