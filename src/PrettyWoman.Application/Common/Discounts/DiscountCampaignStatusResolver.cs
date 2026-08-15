using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Common.Discounts;

public static class DiscountCampaignStatusResolver
{
    public static DiscountCampaignStatusOption Resolve(DiscountCampaign campaign, DateTime now)
    {
        if (campaign.CancelledAt.HasValue) return DiscountCampaignStatusOption.Cancelled;
        if (now < campaign.StartDate) return DiscountCampaignStatusOption.Scheduled;

        return now <= campaign.EndDate
            ? DiscountCampaignStatusOption.Active
            : DiscountCampaignStatusOption.Finished;
    }
}
