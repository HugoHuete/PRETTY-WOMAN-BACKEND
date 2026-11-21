using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PrettyWoman.Application.DTOs.PublicCatalog;
using PrettyWoman.Application.Services;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableCors("PublicCatalog")]
[Route("api/v1/catalog/selections")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SharedCatalogSelectionsController(SharedCatalogSelectionService selections) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("CatalogSelectionCreation")]
    [RequestSizeLimit(8192)]
    public async Task<ActionResult<SelectionLinkDTO>> Create([FromBody] CreateSelectionDTO request,
        [FromHeader(Name = "Idempotency-Key")] Guid key, [FromQuery] bool revalidateExisting, CancellationToken cancellationToken)
    {
        if (key == Guid.Empty || request.Items is not { Count: > 0 and <= SharedCatalogSelectionService.MaximumLines } items ||
            items.Any(item => item is null || item.VariantId <= 0 || item.Quantity <= 0) ||
            items.Select(item => item.VariantId).Distinct().Count() != items.Count)
            return BadRequest(new { message = "La selección debe incluir variantes únicas y cantidades válidas." });

        var result = await selections.CreateAsync(key, items, cancellationToken, revalidateExisting);
        if (result.Link is not null)
            return Ok(result.Link);

        return Conflict(new { result.KeyConflict, result.Unavailable });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SelectionDetailDTO>> Get(Guid id, CancellationToken cancellationToken)
    {
        var (detail, expired) = await selections.GetAsync(id, cancellationToken);
        if (detail is not null)
            return Ok(detail);
        return expired ? StatusCode(StatusCodes.Status410Gone) : NotFound();
    }
}
