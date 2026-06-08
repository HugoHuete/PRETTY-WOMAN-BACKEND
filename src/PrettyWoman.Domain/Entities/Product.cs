namespace PrettyWoman.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductDetailId { get; set; }
    public int SizeId { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitCostWithShipping { get; set; }
    public decimal SalePrice { get; set; }




    public Order? Order { get; set; }
    public ProductDetail? ProductDetail { get; set; }
    public Size? Size { get; set; }
    public List<InventoryMovement> InventoryMovements {get;set;} = [];
}