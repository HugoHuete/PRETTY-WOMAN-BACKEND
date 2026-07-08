namespace PrettyWoman.Application.DTOs.Discounts;

public class DiscountCampaignProductDTO
{
    public int Id { get; set; }
    public int ProductDetailId { get; set; }
    public string? ProductName { get; set; }
    public int? ProductCode { get; set; }
    public int DiscountTypeId { get; set; }
    public string? DiscountTypeName { get; set; }
    public decimal DiscountValue { get; set; }
}