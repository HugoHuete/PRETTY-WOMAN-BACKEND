using AutoMapper;
using PrettyWoman.Application.DTOs.Suppliers;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Mappings;

public class SupplierProfile : Profile
{
    public SupplierProfile()
    {
        CreateMap<CreateSupplierDTO, Supplier>();
        CreateMap<Supplier, SupplierDTO>();
    }
}
