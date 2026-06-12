using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.LoanOwners;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LoanOwnersController(ILoanOwnerService loanOwnerService) : ControllerBase
{
    private readonly ILoanOwnerService _loanOwnerService = loanOwnerService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanOwnerDTO>>> GetAll()
    {
        var loanOwners = await _loanOwnerService.GetAllAsync();
        return Ok(loanOwners);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoanOwnerDTO>> GetById(int id)
    {
        var loanOwner = await _loanOwnerService.GetByIdAsync(id);
        return Ok(loanOwner);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateLoanOwnerDTO createLoanOwnerDTO)
    {
        var id = await _loanOwnerService.CreateAsync(createLoanOwnerDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLoanOwnerDTO updateLoanOwnerDTO)
    {
        await _loanOwnerService.UpdateAsync(id, updateLoanOwnerDTO);
        return NoContent();
    }
}
