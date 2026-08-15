using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Discounts;

namespace PrettyWoman.Application.Interfaces;

public interface IDiscountCampaignService
{
    Task<DiscountCampaignDTO> GetByIdAsync(int id);
    Task<PaginatedResult<DiscountCampaignSummaryDTO>> GetAllAsync(DiscountCampaignQueryDTO query);
    Task<int> CreateAsync(CreateDiscountCampaignDTO createDiscountCampaignDTO);
    Task UpdateAsync(int id, UpdateDiscountCampaignDTO updateDiscountCampaignDTO);
    Task CancelAsync(int id);
    Task ReactivateAsync(int id);
}
