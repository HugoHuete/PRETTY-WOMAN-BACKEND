namespace PrettyWoman.Domain.Entities;

public class SizeGroup
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Size> Sizes { get; set; } = [];
}