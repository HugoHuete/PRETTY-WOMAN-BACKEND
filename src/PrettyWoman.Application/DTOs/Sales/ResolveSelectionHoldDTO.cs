using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.DTOs.Sales;

public class ResolveSelectionHoldDTO
{
    public bool Selected { get; set; }
    public decimal DiscountAmount { get; set; }
    public int DiscountSourceId { get; set; } = (int)DiscountSourceOption.None;
    public int? DiscountCampaignId { get; set; }
    public string? Comments { get; set; }
}
