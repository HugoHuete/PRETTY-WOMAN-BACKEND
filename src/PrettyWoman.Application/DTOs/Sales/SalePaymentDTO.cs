namespace PrettyWoman.Application.DTOs.Sales;

public class SalePaymentDTO
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public int PaymentMethodId { get; set; }
    public string? PaymentMethodName { get; set; }
    public int? PaymentTerminalId { get; set; }
    public string? PaymentTerminalName { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal IncomeTaxPercentage { get; set; }
    public decimal IncomeTaxAmount { get; set; }
    public decimal NetReceivedAmount { get; set; }
}
