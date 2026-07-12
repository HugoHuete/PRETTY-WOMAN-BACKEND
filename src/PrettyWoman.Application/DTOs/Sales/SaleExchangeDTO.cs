namespace PrettyWoman.Application.DTOs.Sales;

public class SaleExchangeDTO
{
    public int Id { get; set; }
    public int OriginalSaleId { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal RecognizedReturnTotal { get; set; }
    public decimal OutboundItemsTotal { get; set; }
    public decimal BalanceToCollect { get; set; }
    public decimal NetGrossProfit { get; set; }
    public string? Comments { get; set; }
    public List<SaleExchangeReturnItemDTO> ReturnItems { get; set; } = [];
    public List<SaleExchangeOutboundItemDTO> OutboundItems { get; set; } = [];
}

public class SaleExchangeReturnItemDTO
{
    public int Id { get; set; }
    public int OriginalSaleProductId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal RecognizedUnitAmount { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class SaleExchangeOutboundItemDTO
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int ItemTypeId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TotalCost { get; set; }
    public bool Delivered { get; set; }
}
