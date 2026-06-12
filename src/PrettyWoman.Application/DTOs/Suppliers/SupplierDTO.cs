using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Suppliers;

public class SupplierDTO
{
    [Required]
    public required string Name { get; set; }
    [Url]
    public string? Url { get; set; }
}