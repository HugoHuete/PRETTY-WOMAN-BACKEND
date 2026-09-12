namespace PrettyWoman.Application.DTOs.Finances;

public class CurrentExchangeRateDTO
{
    public decimal StoreRate { get; set; }
    public decimal BankRate { get; set; }
    public DateTime StartDate { get; set; }
}
