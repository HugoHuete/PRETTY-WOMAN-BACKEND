namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSaleExchangeSettlementDTO
{
    public DateTime? MovementDate { get; set; }
    public string? Comments { get; set; }
    public List<CreateSaleExchangeSettlementTenderDTO> Tenders { get; set; } = [];
}

public class CreateSaleExchangeSettlementTenderDTO
{
    public int PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public decimal Amount { get; set; }
}
