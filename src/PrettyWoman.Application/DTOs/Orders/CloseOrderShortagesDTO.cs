using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class CloseOrderShortagesDTO
{
    public DateTime? ClosedAt { get; set; }
    [StringLength(300)]
    public string? Comments { get; set; }
    [MinLength(1, ErrorMessage = "Debe enviar al menos un faltante.")]
    public ICollection<CloseOrderShortageItemDTO> Items { get; set; } = [];
}

public class CloseOrderShortageItemDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "El producto es requerido.")]
    public int ProductId { get; set; }
    [StringLength(300)]
    public string? Comments { get; set; }
}
