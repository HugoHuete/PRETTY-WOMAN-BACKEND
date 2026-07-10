using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSaleDTO
{
    public DateTime? SaleDate { get; set; }
    public int SaleChannelId { get; set; }
    public int SaleStatusId { get; set; } = (int)SaleStatusOption.Pending;
    public int? ClientId { get; set; }
    public int? MunicipalityId { get; set; }
    public string? Comments { get; set; }
    public List<CreateSaleProductDTO> Products { get; set; } = [];
    public List<CreateSalePaymentDTO> Payments { get; set; } = [];
}
