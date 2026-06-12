using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Sizes;

public class SizeDTO
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public int DisplayOrder { get; set; }
}
