namespace PrettyWoman.Application.DTOs.Sales;

public class SaleDTO
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; }
    public int SaleChannelId { get; set; }
    public string? SaleChannelName { get; set; }
    public int SaleStatusId { get; set; }
    public string? SaleStatusName { get; set; }
    public int SalePaymentStatusId { get; set; }
    public string? SalePaymentStatusName { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal Total { get; set; }
    public string? Comments { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int? MunicipalityId { get; set; }
    public string? MunicipalityName { get; set; }
    public List<SaleProductDTO> Products { get; set; } = [];
    public List<SalePaymentMovementDTO> PaymentMovements { get; set; } = [];
}
