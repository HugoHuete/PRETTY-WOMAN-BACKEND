using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSaleProductDTO
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
    public int DiscountSourceId { get; set; } = (int)DiscountSourceOption.None;
    public int? DiscountCampaignId { get; set; }
}
