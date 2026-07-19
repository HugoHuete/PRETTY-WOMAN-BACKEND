using PrettyWoman.Application.DTOs.InventoryCatalogs;

namespace PrettyWoman.Application.Interfaces;

public interface IInventoryCatalogService
{
    Task<IEnumerable<InventoryCatalogItemDTO>> GetAdjustmentReasonsAsync();
    Task<IEnumerable<InventoryAdjustmentReasonSuggestionDTO>> GetAdjustmentReasonSuggestionsAsync();
    Task<IEnumerable<InventoryCatalogItemDTO>> GetStockBucketsAsync();
}
