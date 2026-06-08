namespace PrettyWoman.Domain.Entities;

public class LoanOwner
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool Enabled { get; set; }

    public List<Loan> Loans { get; set; } = [];
}