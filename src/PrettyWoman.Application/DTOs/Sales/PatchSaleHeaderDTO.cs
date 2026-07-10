using System.Text.Json.Serialization;

namespace PrettyWoman.Application.DTOs.Sales;

public class PatchSaleHeaderDTO
{
    private DateTime? _saleDate;
    private int? _saleChannelId;
    private int? _clientId;
    private string? _comments;

    public DateTime? SaleDate
    {
        get => _saleDate;
        set { _saleDate = value; HasSaleDate = true; }
    }

    public int? SaleChannelId
    {
        get => _saleChannelId;
        set { _saleChannelId = value; HasSaleChannelId = true; }
    }

    public int? ClientId
    {
        get => _clientId;
        set { _clientId = value; HasClientId = true; }
    }

    public string? Comments
    {
        get => _comments;
        set { _comments = value; HasComments = true; }
    }

    [JsonIgnore] public bool HasSaleDate { get; private set; }
    [JsonIgnore] public bool HasSaleChannelId { get; private set; }
    [JsonIgnore] public bool HasClientId { get; private set; }
    [JsonIgnore] public bool HasComments { get; private set; }
}
