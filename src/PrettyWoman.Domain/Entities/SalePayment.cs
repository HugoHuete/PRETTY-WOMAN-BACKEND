namespace PrettyWoman.Domain.Entities;

public class SalePayment
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int SaleId { get; set; }
    public int PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public decimal Amount { get; set; }
    public decimal ComissionAmount { get; set; }
    public decimal NetReceivedAmount { get; set; }
    public required string UserId { get; set; }


    public Sale? Sale { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentTerminal? PaymentTerminal { get; set; }

}