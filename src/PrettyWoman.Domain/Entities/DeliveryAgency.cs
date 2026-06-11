namespace PrettyWoman.Domain.Entities;

public class DeliveryAgency
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public bool Enabled { get; set; } = true;

    public ICollection<SaleDelivery> SaleDeliveries { get; set; } = [];
}