namespace PrettyWoman.Application.DTOs.Sales;

public class SaleProductDTO
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCostAtSale { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public int DiscountSourceId { get; set; }
    public string? DiscountSourceName { get; set; }
    public int? DiscountCampaignId { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TotalCostAtSale { get; set; }
    public decimal GrossProfit { get; set; }
    public int SaleProductStatusId { get; set; }
    public string? SaleProductStatusName { get; set; }
}
