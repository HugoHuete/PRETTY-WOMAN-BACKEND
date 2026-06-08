namespace PrettyWoman.Domain.Entities;

public class Sale
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int SaleChannelId { get; set; }
    public int SaleStatusId { get; set; }
    public required string UserId { get; set; }
    public decimal SubtotalBeforeDiscount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal SubTotal { get; set; } // SubtotalBeforeDiscount - TotalDiscount
    public decimal Comission { get; set; } // from card payments
    public decimal Total { get; set; } // SubTotal - Comission
    public string? Comments { get; set; }
    public int? ClientId { get; set; }
    public int? MunicipalityId { get; set; }


    public SaleStatus? SaleStatus { get; set; }
    public SaleChannel? SaleChannel { get; set; }
    public Client? Client { get; set; }
    public Municipality? Municipality { get; set; }
    public List<SaleProduct> Products { get; set; } = [];
    public List<SalePayment> Payments { get; set; } = [];
    public List<SaleDelivery> Deliveries { get; set; } = [];
}