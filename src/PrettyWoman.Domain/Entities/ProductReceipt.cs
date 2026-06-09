namespace PrettyWoman.Domain.Entities;

public class ProductReceipt
{
    public int Id { get; set; }
    public DateTime ReceivedDate { get; set; }

    public ICollection<OrderTrackingNumber> OrderTrackingNumbers = [];
}