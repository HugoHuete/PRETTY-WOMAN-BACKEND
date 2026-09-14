using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.ShippingCompanies;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/shipping-companies")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class ShippingCompaniesController(IShippingCompanyService shippingCompanyService) : ControllerBase
{
    private readonly IShippingCompanyService _shippingCompanyService = shippingCompanyService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShippingCompanyDTO>>> GetAll()
    {
        var shippingCompanies = await _shippingCompanyService.GetAllAsync();
        return Ok(shippingCompanies);
    }
}
