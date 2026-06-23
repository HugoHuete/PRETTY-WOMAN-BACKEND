using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Loans;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LoansController(ILoanService loanService) : ControllerBase
{
    private readonly ILoanService _loanService = loanService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<LoanDTO>>> GetAll([FromQuery] LoanQueryDTO query)
    {
        var loans = await _loanService.GetAllAsync(query);
        return Ok(loans);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoanDTO>> GetById(int id)
    {
        var loan = await _loanService.GetByIdAsync(id);
        return Ok(loan);
    }

    [HttpPost]
    public async Task<ActionResult<LoanDTO>> Create([FromBody] CreateLoanDTO createLoanDTO)
    {
        var loan = await _loanService.CreateAsync(createLoanDTO);
        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LoanDTO>> Update(int id, [FromBody] UpdateLoanDTO updateLoanDTO)
    {
        var loan = await _loanService.UpdateAsync(id, updateLoanDTO);
        return Ok(loan);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _loanService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<LoanDTO>> Pay(int id, [FromBody] PayLoanDTO payLoanDTO)
    {
        var loan = await _loanService.PayAsync(id, payLoanDTO);
        return Ok(loan);
    }
}
