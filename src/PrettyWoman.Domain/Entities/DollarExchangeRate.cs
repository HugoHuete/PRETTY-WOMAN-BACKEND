namespace PrettyWoman.Domain.Entities;

public class DollarExchangeRate
{
    public int Id { get; set; }
    public decimal StoreRate { get; set; }
    public decimal BankRate { get; set; }
    public DateTime StartDate { get; set; }
    public bool Enabled { get; set; } = true;
}