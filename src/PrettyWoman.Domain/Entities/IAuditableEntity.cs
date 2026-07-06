namespace PrettyWoman.Domain.Entities;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? CreatedById { get; set; }
    string? UpdatedById { get; set; }
}