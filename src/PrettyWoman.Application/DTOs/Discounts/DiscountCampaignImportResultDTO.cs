namespace PrettyWoman.Application.DTOs.Discounts;

public class DiscountCampaignImportResultDTO
{
    public bool IsValid { get; set; }
    public List<DiscountCampaignImportRowResultDTO> Rows { get; set; } = [];
}

public class DiscountCampaignImportRowResultDTO
{
    public int RowNumber { get; set; }
    public int ProductCode { get; set; }
    public int? ProductId { get; set; }
    public int? ProductPresentationId { get; set; }
    public string? ProductName { get; set; }
    public string? PresentationName { get; set; }
    public int DiscountTypeId { get; set; }
    public decimal DiscountValue { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
}
