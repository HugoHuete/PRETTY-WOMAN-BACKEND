using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.PaymentTerminals;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class PaymentTerminalService(IApplicationDbContext context, IMapper mapper) : IPaymentTerminalService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreatePaymentTerminalDTO createPaymentTerminalDTO)
    {
        createPaymentTerminalDTO.Name = createPaymentTerminalDTO.Name.NormalizeRequired("Payment terminal name");

        var exists = await _context.PaymentTerminals
            .AnyAsync(paymentTerminal => paymentTerminal.Name.ToLower() == createPaymentTerminalDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A payment terminal with that name already exists.");
        }

        var paymentTerminal = _mapper.Map<PaymentTerminal>(createPaymentTerminalDTO);

        await _context.PaymentTerminals.AddAsync(paymentTerminal);
        await _context.SaveChangesAsync();

        return paymentTerminal.Id;
    }

    public async Task UpdateAsync(int id, UpdatePaymentTerminalDTO updatePaymentTerminalDTO)
    {
        var paymentTerminal = await _context.PaymentTerminals.FirstOrDefaultAsync(paymentTerminal => paymentTerminal.Id == id)
            ?? throw new AppNotFoundException($"Payment terminal with id '{id}' does not exist.");

        updatePaymentTerminalDTO.Name = updatePaymentTerminalDTO.Name.NormalizeRequired("Payment terminal name");

        var exists = await _context.PaymentTerminals
            .AnyAsync(paymentTerminal => paymentTerminal.Id != id && paymentTerminal.Name.ToLower() == updatePaymentTerminalDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A payment terminal with that name already exists.");
        }

        _mapper.Map(updatePaymentTerminalDTO, paymentTerminal);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<PaymentTerminalDTO>> GetAllAsync()
    {
        var paymentTerminals = await _context.PaymentTerminals
            .OrderBy(paymentTerminal => paymentTerminal.Name)
            .ToListAsync();

        return _mapper.Map<List<PaymentTerminalDTO>>(paymentTerminals);
    }

    public async Task<PaymentTerminalDTO> GetByIdAsync(int id)
    {
        var paymentTerminal = await _context.PaymentTerminals.FirstOrDefaultAsync(paymentTerminal => paymentTerminal.Id == id)
            ?? throw new AppNotFoundException($"Payment terminal with id '{id}' does not exist.");

        return _mapper.Map<PaymentTerminalDTO>(paymentTerminal);
    }
}
