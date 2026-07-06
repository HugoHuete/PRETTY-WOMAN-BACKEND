namespace PrettyWoman.Domain.Entities;

public class SalePayment : IAuditableEntity
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public int SaleId { get; set; }
    public int PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public decimal GrossAmount { get; set; }
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
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentTerminal? PaymentTerminal { get; set; }

}