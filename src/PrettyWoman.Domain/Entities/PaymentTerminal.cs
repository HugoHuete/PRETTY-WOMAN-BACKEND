namespace PrettyWoman.Domain.Entities;

public class PaymentTerminal
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal ComissionPercentage { get; set; }
    public decimal IncomeTaxPercentage { get; set; }
    public bool Enabled { get; set; } = true;
}