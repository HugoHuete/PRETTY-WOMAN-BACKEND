namespace PrettyWoman.Application.DTOs.Municipalities;

public class MunicipalityDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DepartmentId { get; set; }
    public required string DepartmentName { get; set; }
}
