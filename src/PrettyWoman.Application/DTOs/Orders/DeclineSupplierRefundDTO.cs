using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class DeclineSupplierRefundDTO
{
    public DateTime? DeclinedAt { get; set; }

    [StringLength(300)]
    public string? Comments { get; set; }
}
