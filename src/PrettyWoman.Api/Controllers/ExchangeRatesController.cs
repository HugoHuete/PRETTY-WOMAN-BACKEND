using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Finances;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/exchange-rates")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class ExchangeRatesController(IExchangeRateService exchangeRateService) : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService = exchangeRateService;

    [HttpGet("current")]
    public async Task<ActionResult<CurrentExchangeRateDTO>> GetCurrent()
    {
        var exchangeRate = await _exchangeRateService.GetCurrentAsync();
        return Ok(exchangeRate);
    }
}
