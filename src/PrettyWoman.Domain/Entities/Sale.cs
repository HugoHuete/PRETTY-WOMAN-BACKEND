using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

public class Sale : IAuditableEntity
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; }
    public int SaleChannelId { get; set; }
    public int SaleStatusId { get; set; } = (int)SaleStatusOption.Pending;
    public int SalePaymentStatusId { get; set; } = (int)SalePaymentStatusOption.Unpaid;
    public required string UserId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal Total { get; set; } // Subtotal - TotalDiscount
    public string? Comments { get; set; }
    public int? ClientId { get; set; }
    public int? MunicipalityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }


    public SaleStatus? SaleStatus { get; set; }
    public SalePaymentStatus? SalePaymentStatus { get; set; }
    public SaleChannel? SaleChannel { get; set; }
    public Client? Client { get; set; }
    public Municipality? Municipality { get; set; }
    public List<SaleProduct> Products { get; set; } = [];
    public List<SalePayment> Payments { get; set; } = [];
    public List<SaleDelivery> Deliveries { get; set; } = [];
}
