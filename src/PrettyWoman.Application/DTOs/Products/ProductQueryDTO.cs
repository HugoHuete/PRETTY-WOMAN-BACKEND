namespace PrettyWoman.Application.DTOs.Products;

public class ProductQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public ProductAvailabilityFilter? Availability { get; set; }
    public int? CategoryId { get; set; }
    public int? SubcategoryId { get; set; }
    public int? SizeId { get; set; }
}
