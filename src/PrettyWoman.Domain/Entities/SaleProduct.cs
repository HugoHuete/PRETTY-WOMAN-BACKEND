namespace PrettyWoman.Domain.Entities;

public class SaleProduct
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCostAtSale { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public int DiscountSourceId { get; set; }
    public int? DiscountCampaignId { get; set; }
    public decimal DiscountAmount { get; set; } = 0; // Discount by unit
    public decimal FinalUnitPrice { get; set; } // OriginalSalePrice - DiscountAmount
    public decimal LineTotal { get; set; } // FinalSalePrice * Quantity
    public decimal TotalCostAtSale { get; set; }
    public decimal GrossProfit { get; set; } // LineTotal - TotalCostAtSale
    public int SaleProductStatusId { get; set; }



    public Sale? Sale { get; set; }
    public Product? Product { get; set; }
    public DiscountSource? DiscountSource { get; set; }
    public DiscountCampaign? DiscountCampaign { get; set; }
    public SaleProductStatus? SaleProductStatus { get; set; }
}
