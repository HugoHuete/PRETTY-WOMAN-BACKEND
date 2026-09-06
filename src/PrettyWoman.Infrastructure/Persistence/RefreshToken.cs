namespace PrettyWoman.Infrastructure.Persistence;

public class RefreshToken
{
    public Guid Id { get; set; }
    // Agrupa la cadena de tokens de una misma sesión o dispositivo.
    // No se expone al cliente ni cambia cuando el token rota.
    public Guid FamilyId { get; set; }
    public required string UserId { get; set; }
    public required string TokenHash { get; set; }
    public required string SecurityStamp { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    // Solo existe durante una ventana breve para responder de forma idempotente
    // a dos refresh simultáneos. El valor está protegido, no almacenado en texto plano.
    public string? ReplacementTokenProtected { get; set; }
    public DateTime? ReplayUntilUtc { get; set; }
    public required User User { get; set; }
}
