namespace PrettyWoman.Application.DTOs.Sales;

public class CreateMigrationSaleDTO : CreateSaleDTO
{
    // One final unit price for each ProductVariants entry, in the same order.
    public List<decimal> FinalUnitPrices { get; set; } = [];
}
