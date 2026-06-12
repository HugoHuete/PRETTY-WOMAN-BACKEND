using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Suppliers;

public class SupplierDTO
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Nombre del proveedor es obligatorio.")]
    public required string Name { get; set; }
    [Url(ErrorMessage = "La URL del proveedor no tiene un formato válido.")]
    public string? Url { get; set; }
}
