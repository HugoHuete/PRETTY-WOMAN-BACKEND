namespace PrettyWoman.Application.DTOs.Products;

public class UpdateProductImagesDTO
{
    public int PrimaryImageId { get; set; }
    public required List<int> ImageIdsInOrder { get; set; }
}
