namespace PrettyWoman.Domain.Entities;

public class Client
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? InstagramUser { get; set; }
    public string? MessengerUser { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsBlocked { get; set; } = false;
    public bool IsFriend { get; set; } = false;
    public string? BlockedReason { get; set; }
    public string? Comments { get; set; }


    public ICollection<Sale> Sales { get; set; } = [];
}