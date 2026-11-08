namespace PrettyWoman.Domain.Entities;

/// <summary>
/// Records one payment-method portion of an exchange settlement and its related financial movement.
/// A settlement has one tender per payment method used, allowing mixed payments such as cash and card.
/// </summary>
public class SaleExchangeSettlementTender : IAuditableEntity
{
    public int Id { get; set; }
    public int SaleExchangeSettlementId { get; set; }
    public int PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal IncomeTaxPercentage { get; set; }
    public decimal IncomeTaxAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public SaleExchangeSettlement? Settlement { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentTerminal? PaymentTerminal { get; set; }
    public FinancialMovement? FinancialMovement { get; set; }
}
