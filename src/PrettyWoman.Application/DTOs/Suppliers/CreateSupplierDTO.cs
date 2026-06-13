using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Suppliers;

public class CreateSupplierDTO
{
    [Required(ErrorMessage = "Nombre del proveedor es obligatorio.")]
    public required string Name { get; set; }
    [Url(ErrorMessage = "La URL del proveedor no tiene un formato válido.")]
    public string? Url { get; set; }

    public bool IsNational { get; set; } = true;
    public bool Enabled { get; set; } = true;
}
