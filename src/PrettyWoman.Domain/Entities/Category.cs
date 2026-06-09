namespace PrettyWoman.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Subcategory> Subcategories { get; set; } = [];
}