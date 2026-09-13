using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Finances;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Application.Services;

public class ExchangeRateService(IApplicationDbContext context) : IExchangeRateService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<CurrentExchangeRateDTO> GetCurrentAsync()
    {
        var rate = await _context.DollarExchangeRates
            .AsNoTracking()
            .Where(rate => rate.Enabled)
            .OrderByDescending(rate => rate.StartDate)
            .ThenByDescending(rate => rate.Id)
            .FirstOrDefaultAsync()
            ?? throw new AppNotFoundException("No existe una tasa de cambio bancaria habilitada.");

        return new CurrentExchangeRateDTO
        {
            StoreRate = rate.StoreRate,
            BankRate = rate.BankRate,
            StartDate = rate.StartDate
        };
    }
}
