using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Discounts;

public class CreateDiscountCampaignDTO
{
    [Required(ErrorMessage = "Nombre de la campania de descuento es obligatorio.")]
    public required string Name { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }
    public bool Enabled { get; set; } = true;
    public List<CreateDiscountCampaignProductDTO> Products { get; set; } = [];
}
