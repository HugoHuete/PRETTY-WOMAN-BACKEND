namespace PrettyWoman.Domain.Entities;

public class ExpenseCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool Enabled { get; set; }
}