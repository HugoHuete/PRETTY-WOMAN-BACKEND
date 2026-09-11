namespace PrettyWoman.Application.DTOs.Orders;

public class OrderProductPresentationDTO
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int SortOrder { get; set; }
    public ICollection<OrderProductVariantDTO> Sizes { get; set; } = [];
}
