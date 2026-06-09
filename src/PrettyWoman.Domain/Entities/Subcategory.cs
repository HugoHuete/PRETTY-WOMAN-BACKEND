namespace PrettyWoman.Domain.Entities;

public class Subcategory
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public required string Name { get; set; }


    public Category? Category { get; set; }
    public ICollection<ProductDetail> ProductDetails { get; set; } = [];
}