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
public class ProductDetailsController(IProductService productService, IProductImageService productImageService) : ControllerBase
{
    private readonly IProductService _productService = productService;
    private readonly IProductImageService _productImageService = productImageService;

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

    [HttpGet("{productDetailId:int}/images/{imageId:int}")]
    public async Task<ActionResult<ProductImageDTO>> GetImageById(int productDetailId, int imageId, CancellationToken cancellationToken)
    {
        var image = await _productImageService.GetByIdAsync(productDetailId, imageId, cancellationToken);
        return Ok(image);
    }

    [HttpPost("{productDetailId:int}/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<ActionResult<ProductImageDTO>> UploadImage(int productDetailId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var image = await _productImageService.UploadAsync(productDetailId, content, file.ContentType, cancellationToken);
        return CreatedAtAction(nameof(GetImageById), new { productDetailId, imageId = image.Id }, image);
    }

    [HttpPut("{productDetailId:int}/images")]
    public async Task<ActionResult<IReadOnlyCollection<ProductImageDTO>>> UpdateImages(
        int productDetailId,
        UpdateProductImagesDTO request,
        CancellationToken cancellationToken)
    {
        var images = await _productImageService.UpdateAsync(productDetailId, request, cancellationToken);
        return Ok(images);
    }

    [HttpDelete("{productDetailId:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int productDetailId, int imageId, CancellationToken cancellationToken)
    {
        await _productImageService.DeleteAsync(productDetailId, imageId, cancellationToken);
        return NoContent();
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
