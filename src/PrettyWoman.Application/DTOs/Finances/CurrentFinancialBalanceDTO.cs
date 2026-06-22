namespace PrettyWoman.Application.DTOs.Finances;

public class CurrentFinancialBalanceDTO
{
    public decimal IncomeTotalNio { get; set; }
    public decimal ExpenseTotalNio { get; set; }
    public decimal BalanceNio { get; set; }
    public int MovementCount { get; set; }
    public DateTime? LastMovementAt { get; set; }
}
