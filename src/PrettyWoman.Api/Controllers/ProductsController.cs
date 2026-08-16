using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class ProductsController(IProductService productService, IProductImageService productImageService) : ControllerBase
{
    private readonly IProductService _productService = productService;
    private readonly IProductImageService _productImageService = productImageService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProductDTO>>> GetAll([FromQuery] ProductQueryDTO query)
    {
        var productVariants = await _productService.GetAllAsync(query);
        return Ok(productVariants);
    }

    [HttpGet("{productId:int}")]
    public async Task<ActionResult<ProductDTO>> GetById(int productId)
    {
        var productVariant = await _productService.GetByIdAsync(productId);
        return Ok(productVariant);
    }

    [HttpPatch("{productId:int}/variants/{productVariantId:int}/price")]
    public async Task<IActionResult> UpdatePrice(
        int productId,
        int productVariantId,
        UpdateProductPriceDTO request)
    {
        await _productService.UpdatePriceAsync(productId, productVariantId, request);
        return NoContent();
    }

    [HttpGet("{productId:int}/images/{imageId:int}")]
    public async Task<ActionResult<ProductImageDTO>> GetImageById(int productId, int imageId, CancellationToken cancellationToken)
    {
        var image = await _productImageService.GetByIdAsync(productId, imageId, cancellationToken);
        return Ok(image);
    }

    [HttpPost("{productId:int}/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<ActionResult<ProductImageDTO>> UploadImage(int productId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var image = await _productImageService.UploadAsync(productId, content, file.ContentType, cancellationToken);
        return CreatedAtAction(nameof(GetImageById), new { productId, imageId = image.Id }, image);
    }

    [HttpPut("{productId:int}/images")]
    public async Task<ActionResult<IReadOnlyCollection<ProductImageDTO>>> UpdateImages(
        int productId,
        UpdateProductImagesDTO request,
        CancellationToken cancellationToken)
    {
        var images = await _productImageService.UpdateAsync(productId, request, cancellationToken);
        return Ok(images);
    }

    [HttpDelete("{productId:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int productId, int imageId, CancellationToken cancellationToken)
    {
        await _productImageService.DeleteAsync(productId, imageId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{productId:int}/inventory-movements")]
    public async Task<ActionResult<IEnumerable<ProductInventoryMovementDTO>>> GetInventoryMovements(int productId)
    {
        var movements = await _productService.GetInventoryMovementsAsync(productId);
        return Ok(movements);
    }

    [HttpGet("{productId:int}/variants/{productVariantId:int}/inventory-movements")]
    public async Task<ActionResult<IEnumerable<ProductInventoryMovementDTO>>> GetVariantInventoryMovements(int productId, int productVariantId)
    {
        var movements = await _productService.GetInventoryMovementsAsync(productId, productVariantId);
        return Ok(movements);
    }
}
