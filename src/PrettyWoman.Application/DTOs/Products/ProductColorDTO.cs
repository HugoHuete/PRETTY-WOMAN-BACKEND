using System.Text.Json.Serialization;

namespace PrettyWoman.Application.DTOs.Products;

public sealed record ProductColorDTO(int Id, string Name, string HexCode);
public sealed record UpdatePresentationColorDTO([property: JsonRequired] int? ColorId);
