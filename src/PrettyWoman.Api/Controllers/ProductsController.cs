using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProductDetailDTO>>> GetAll([FromQuery] ProductQueryDTO query)
    {
        var products = await _productService.GetAllAsync(query);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailDTO>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        return Ok(product);
    }
}
