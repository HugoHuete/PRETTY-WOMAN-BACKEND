using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Municipalities;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/municipalities")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class MunicipalitiesController(IMunicipalityService municipalityService) : ControllerBase
{
    private readonly IMunicipalityService _municipalityService = municipalityService;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MunicipalityDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MunicipalityDTO>>> GetAll([FromQuery] int? departmentId = null)
    {
        var municipalities = await _municipalityService.GetAllAsync(departmentId);
        return Ok(municipalities);
    }
}
