namespace PrettyWoman.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public List<Subcategory> Subcategories { get; set; } = [];
}