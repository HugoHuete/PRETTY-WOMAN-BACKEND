using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.LoanOwners;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class LoanOwnerService(IApplicationDbContext context, IMapper mapper) : ILoanOwnerService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateLoanOwnerDTO createLoanOwnerDTO)
    {
        createLoanOwnerDTO.Name = createLoanOwnerDTO.Name.NormalizeRequired("Loan owner name");

        var exists = await _context.LoanOwners
            .AnyAsync(loanOwner => loanOwner.Name.ToLower() == createLoanOwnerDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A loan owner with that name already exists.");
        }

        var loanOwner = _mapper.Map<LoanOwner>(createLoanOwnerDTO);

        await _context.LoanOwners.AddAsync(loanOwner);
        await _context.SaveChangesAsync();

        return loanOwner.Id;
    }

    public async Task UpdateAsync(int id, UpdateLoanOwnerDTO updateLoanOwnerDTO)
    {
        var loanOwner = await _context.LoanOwners.FirstOrDefaultAsync(loanOwner => loanOwner.Id == id)
            ?? throw new AppNotFoundException($"Loan owner with id '{id}' does not exist.");

        updateLoanOwnerDTO.Name = updateLoanOwnerDTO.Name.NormalizeRequired("Loan owner name");

        var exists = await _context.LoanOwners
            .AnyAsync(loanOwner => loanOwner.Id != id && loanOwner.Name.ToLower() == updateLoanOwnerDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A loan owner with that name already exists.");
        }

        _mapper.Map(updateLoanOwnerDTO, loanOwner);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<LoanOwnerDTO>> GetAllAsync()
    {
        var loanOwners = await _context.LoanOwners
            .OrderBy(loanOwner => loanOwner.Name)
            .ToListAsync();

        return _mapper.Map<List<LoanOwnerDTO>>(loanOwners);
    }

    public async Task<LoanOwnerDTO> GetByIdAsync(int id)
    {
        var loanOwner = await _context.LoanOwners.FirstOrDefaultAsync(loanOwner => loanOwner.Id == id)
            ?? throw new AppNotFoundException($"Loan owner with id '{id}' does not exist.");

        return _mapper.Map<LoanOwnerDTO>(loanOwner);
    }
}
