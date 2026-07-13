using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Clients;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class ClientsController(IClientService clientService) : ControllerBase
{
    private readonly IClientService _clientService = clientService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ClientDTO>>> GetAll([FromQuery] ClientQueryDTO query)
    {
        var clients = await _clientService.GetAllAsync(query);
        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientDTO>> GetById(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateClientDTO createClientDTO)
    {
        var id = await _clientService.CreateAsync(createClientDTO);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientDTO updateClientDTO)
    {
        await _clientService.UpdateAsync(id, updateClientDTO);
        return NoContent();
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPatch("{id:int}/block")]
    public async Task<IActionResult> Block(int id, [FromBody] BlockClientDTO blockClientDTO)
    {
        await _clientService.BlockAsync(id, blockClientDTO);
        return NoContent();
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPatch("{id:int}/unblock")]
    public async Task<IActionResult> Unblock(int id)
    {
        await _clientService.UnblockAsync(id);
        return NoContent();
    }
}
