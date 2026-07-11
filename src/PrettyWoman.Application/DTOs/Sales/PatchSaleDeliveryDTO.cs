using System.Text.Json.Serialization;

namespace PrettyWoman.Application.DTOs.Sales;

public class PatchSaleDeliveryDTO
{
    private int? _clientId;
    private string? _deliveryAddress;
    private int? _deliveryAgencyId;
    private int? _municipalityId;
    private string? _code;
    private decimal? _shippingChargedToClient;

    public int? ClientId
    {
        get => _clientId;
        set { _clientId = value; HasClientId = true; }
    }

    public string? DeliveryAddress
    {
        get => _deliveryAddress;
        set { _deliveryAddress = value; HasDeliveryAddress = true; }
    }

    public int? DeliveryAgencyId
    {
        get => _deliveryAgencyId;
        set { _deliveryAgencyId = value; HasDeliveryAgencyId = true; }
    }

    public int? MunicipalityId
    {
        get => _municipalityId;
        set { _municipalityId = value; HasMunicipalityId = true; }
    }

    public string? Code
    {
        get => _code;
        set { _code = value; HasCode = true; }
    }

    public decimal? ShippingChargedToClient
    {
        get => _shippingChargedToClient;
        set { _shippingChargedToClient = value; HasShippingChargedToClient = true; }
    }

    [JsonIgnore] public bool HasClientId { get; private set; }
    [JsonIgnore] public bool HasDeliveryAddress { get; private set; }
    [JsonIgnore] public bool HasDeliveryAgencyId { get; private set; }
    [JsonIgnore] public bool HasMunicipalityId { get; private set; }
    [JsonIgnore] public bool HasCode { get; private set; }
    [JsonIgnore] public bool HasShippingChargedToClient { get; private set; }
}