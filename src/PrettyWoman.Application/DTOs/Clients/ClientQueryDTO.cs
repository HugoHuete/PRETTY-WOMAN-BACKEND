namespace PrettyWoman.Application.DTOs.Clients;

public class ClientQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? InstagramUser { get; set; }
    public string? MessengerUser { get; set; }
}