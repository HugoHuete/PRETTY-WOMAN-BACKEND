using PrettyWoman.Application.DTOs.Discounts;

namespace PrettyWoman.Application.Interfaces;

public interface IDiscountCampaignService
{
    Task<DiscountCampaignDTO> GetByIdAsync(int id);
    Task<IEnumerable<DiscountCampaignDTO>> GetAllAsync(bool? enabled = null);
    Task<int> CreateAsync(CreateDiscountCampaignDTO createDiscountCampaignDTO);
    Task UpdateAsync(int id, UpdateDiscountCampaignDTO updateDiscountCampaignDTO);
    Task DisableAsync(int id);
}
