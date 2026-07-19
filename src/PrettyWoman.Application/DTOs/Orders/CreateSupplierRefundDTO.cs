using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class CreateSupplierRefundDTO
{
    [Range(typeof(decimal), "0.01", "999999999999", ErrorMessage = "El monto reembolsado debe ser mayor que cero.")]
    public decimal AmountNio { get; set; }
    public DateTime? RefundedAt { get; set; }
    [StringLength(100)]
    public string? Reference { get; set; }
    [StringLength(300)]
    public string? Comments { get; set; }
}
