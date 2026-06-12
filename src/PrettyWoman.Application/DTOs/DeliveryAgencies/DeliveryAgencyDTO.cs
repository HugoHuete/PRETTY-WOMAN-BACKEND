using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.DeliveryAgencies;

public class DeliveryAgencyDTO
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string PhoneNumber { get; set; }

    public bool Enabled { get; set; }
}
