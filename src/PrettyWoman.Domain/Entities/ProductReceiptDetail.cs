namespace PrettyWoman.Domain.Entities;

public class ProductReceiptDetail
{
    public int Id { get; set; }
    public int ProductReceiptId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    public ProductReceipt? ProductReceipt { get; set; }
    public Product? Product { get; set; }
}