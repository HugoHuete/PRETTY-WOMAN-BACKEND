namespace PrettyWoman.Domain.Entities;

public class Municipality
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DepartmentId { get; set; }

    public Department? Department { get; set; }
}