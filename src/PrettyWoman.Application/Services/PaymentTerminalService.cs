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
        createPaymentTerminalDTO.Name = createPaymentTerminalDTO.Name.NormalizeRequired("Nombre de la terminal de pago");

        var exists = await _context.PaymentTerminals
            .AnyAsync(paymentTerminal => paymentTerminal.Name.ToLower() == createPaymentTerminalDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una terminal de pago con ese nombre.");
        }

        var paymentTerminal = _mapper.Map<PaymentTerminal>(createPaymentTerminalDTO);

        await _context.PaymentTerminals.AddAsync(paymentTerminal);
        await _context.SaveChangesAsync();

        return paymentTerminal.Id;
    }

    public async Task UpdateAsync(int id, UpdatePaymentTerminalDTO updatePaymentTerminalDTO)
    {
        var paymentTerminal = await _context.PaymentTerminals.FirstOrDefaultAsync(paymentTerminal => paymentTerminal.Id == id)
            ?? throw new AppNotFoundException($"La terminal de pago con id '{id}' no existe.");

        updatePaymentTerminalDTO.Name = updatePaymentTerminalDTO.Name.NormalizeRequired("Nombre de la terminal de pago");

        var exists = await _context.PaymentTerminals
            .AnyAsync(paymentTerminal => paymentTerminal.Id != id && paymentTerminal.Name.ToLower() == updatePaymentTerminalDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una terminal de pago con ese nombre.");
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
            ?? throw new AppNotFoundException($"La terminal de pago con id '{id}' no existe.");

        return _mapper.Map<PaymentTerminalDTO>(paymentTerminal);
    }
}
