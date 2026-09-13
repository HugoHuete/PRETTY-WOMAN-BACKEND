using PrettyWoman.Application.DTOs.Finances;

namespace PrettyWoman.Application.Interfaces;

public interface IExchangeRateService
{
    Task<CurrentExchangeRateDTO> GetCurrentAsync();
}
