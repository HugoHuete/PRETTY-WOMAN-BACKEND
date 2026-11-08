namespace PrettyWoman.Application.DTOs.Sales;

public class SaleExchangeSettlementDTO
{
    public int Id { get; set; }
    public int MovementDirectionId { get; set; }
    public string MovementDirectionName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime MovementDate { get; set; }
    public string? Comments { get; set; }
    public List<SaleExchangeSettlementTenderDTO> Tenders { get; set; } = [];
}

public class SaleExchangeSettlementTenderDTO
{
    public int Id { get; set; }
    public int PaymentMethodId { get; set; }
    public string? PaymentMethodName { get; set; }
    public int? PaymentTerminalId { get; set; }
    public string? PaymentTerminalName { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal IncomeTaxAmount { get; set; }
    public int? FinancialMovementId { get; set; }
}
