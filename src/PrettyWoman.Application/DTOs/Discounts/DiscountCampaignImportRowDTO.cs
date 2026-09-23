namespace PrettyWoman.Application.DTOs.Discounts;

public class DiscountCampaignImportRowDTO
{
    public int RowNumber { get; set; }
    public int ProductCode { get; set; }
    public int? ProductPresentationId { get; set; }
    public int DiscountTypeId { get; set; }
    public decimal DiscountValue { get; set; }
}
