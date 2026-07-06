namespace PrettyWoman.Application.DTOs.Loans;

public class LoanPaymentDTO
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? Comments { get; set; }
}
