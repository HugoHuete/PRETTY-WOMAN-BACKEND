namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSalePaymentDTO
{
    public DateTime? PaymentDate { get; set; }
    public int PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public decimal GrossAmount { get; set; }
}
