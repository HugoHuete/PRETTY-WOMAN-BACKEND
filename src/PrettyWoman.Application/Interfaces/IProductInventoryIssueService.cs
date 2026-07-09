using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products.InventoryIssues;

namespace PrettyWoman.Application.Interfaces;

public interface IProductInventoryIssueService
{
    Task<PaginatedResult<ProductInventoryIssueDTO>> GetAllAsync(ProductInventoryIssueQueryDTO query);
    Task<ProductInventoryIssueDTO> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateProductInventoryIssueDTO createIssueDTO);
    Task<ProductInventoryIssueDTO> ResolveAsync(int id, ResolveProductInventoryIssueDTO resolveIssueDTO);
    Task<ProductInventoryIssueDTO> DeleteAsync(int id);
}