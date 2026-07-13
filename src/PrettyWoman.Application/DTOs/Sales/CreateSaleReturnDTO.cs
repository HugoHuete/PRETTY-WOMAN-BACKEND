namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSaleReturnDTO
{
    public int ReasonId { get; set; }
    public int MethodId { get; set; }
    public int? DeliveryAgencyId { get; set; }
    public string? ReturnCode { get; set; }
    public decimal ReturnShippingChargedToClient { get; set; }
    public decimal ReturnShippingPaidToAgency { get; set; }
    public string? Comments { get; set; }
    public List<CreateSaleReturnItemDTO> Items { get; set; } = [];
}

public class CreateSaleReturnItemDTO
{
    public int OriginalSaleProductId { get; set; }
    public int Quantity { get; set; }
    public decimal RecognizedUnitAmount { get; set; }
    public string? Comments { get; set; }
}

public class ProcessSaleReturnDTO
{
    public int PaymentMethodId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Comments { get; set; }
}

public class ReceiveSaleReturnDTO
{
    public int? PaymentMethodId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Comments { get; set; }
    public List<ReceiveSaleReturnItemDTO> Items { get; set; } = [];
}

public class ReceiveSaleReturnItemDTO
{
    public int SaleReturnItemId { get; set; }
    public bool IsDamaged { get; set; }
    public string? Comments { get; set; }
}
