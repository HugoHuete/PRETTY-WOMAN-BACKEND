using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.DTOs.Discounts;

public class DiscountCampaignQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DiscountCampaignStatusOption? Status { get; set; }
}
