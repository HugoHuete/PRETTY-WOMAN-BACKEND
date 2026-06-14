namespace PrettyWoman.Domain.Entities;

public class Size
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int SizeGroupId { get; set; }
    public int DisplayOrder { get; set; }

    public SizeGroup? SizeGroup { get; set; }
}