namespace PrettyWoman.Domain.Entities;

public class InventoryMovement
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ProductId { get; set; }
    public int MovementDirectionId { get; set; }
    public int InventoryMovementTypeId { get; set; }
    public int Quantity { get; set; }
    public int? OrderId { get; set; }
    public int? SaleProductId { get; set; }
    public int? ProductHoldId { get; set; }
    public int? ProductInventoryIssueId { get; set; }
    public string? Comments { get; set; }




    public MovementDirection? MovementDirection { get; set; }
    public InventoryMovementType? InventoryMovementType { get; set; }
    public Product? Product { get; set; }
    public Order? Order { get; set; }
    public SaleProduct? SaleProduct { get; set; }
    public ProductHold? ProductHold { get; set; }
    public ProductInventoryIssue? ProductInventoryIssue { get; set; }


}
