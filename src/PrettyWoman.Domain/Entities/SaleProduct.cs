namespace PrettyWoman.Domain.Entities;

public class SaleProduct
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal CostAtSale { get; set; }
    public decimal OriginalSalePrice { get; set; }
    public int DiscountSourceId { get; set; }
    public int? DiscountCampaignId { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal FinalSalePrice { get; set; }
    public decimal PaymentComission { get; set; } = 0;
    public decimal GrossProfit { get; set; } // FinalSalePrice - PaymentComission - CostAtSale
    public int SaleProductStatusId { get; set; }



    public Sale? Sale { get; set; }
    public Product? Product { get; set; }
    public DiscountSource? DiscountSource { get; set; }
    public DiscountCampaign? DiscountCampaign { get; set; }
    public SaleProductStatus? SaleProductStatus { get; set; }
}