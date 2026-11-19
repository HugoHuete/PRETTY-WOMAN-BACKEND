using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.PublicCatalog;
using PrettyWoman.Application.Services;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableCors("PublicCatalog")]
[Route("api/v1/catalog")]
public sealed class PublicCatalogController(PublicCatalogService catalog) : ControllerBase
{
    [HttpGet("products")]
    public Task<CatalogPageDTO> List([FromQuery] CatalogQueryDTO query, CancellationToken cancellationToken) =>
        catalog.ListAsync(query, cancellationToken);

    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<CatalogDetailDTO>> Detail(int id, CancellationToken cancellationToken)
    {
        var product = await catalog.GetAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("filters")]
    public Task<CatalogFiltersDTO> Filters(CancellationToken cancellationToken) =>
        catalog.FiltersAsync(cancellationToken);
}
