using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/product-details")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class ProductDetailsController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProductDetailDTO>>> GetAll([FromQuery] ProductQueryDTO query)
    {
        var products = await _productService.GetAllAsync(query);
        return Ok(products);
    }

    [HttpGet("{productDetailId:int}")]
    public async Task<ActionResult<ProductDetailDTO>> GetById(int productDetailId)
    {
        var product = await _productService.GetByIdAsync(productDetailId);
        return Ok(product);
    }

    [HttpGet("{productDetailId:int}/inventory-movements")]
    public async Task<ActionResult<IEnumerable<ProductInventoryMovementDTO>>> GetInventoryMovements(int productDetailId)
    {
        var movements = await _productService.GetInventoryMovementsAsync(productDetailId);
        return Ok(movements);
    }

    [HttpGet("{productDetailId:int}/variants/{productId:int}/inventory-movements")]
    public async Task<ActionResult<IEnumerable<ProductInventoryMovementDTO>>> GetVariantInventoryMovements(int productDetailId, int productId)
    {
        var movements = await _productService.GetInventoryMovementsAsync(productDetailId, productId);
        return Ok(movements);
    }
}
