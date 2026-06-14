using AutoMapper;
using PrettyWoman.Application.DTOs.Categories;
using PrettyWoman.Application.DTOs.Clients;
using PrettyWoman.Application.DTOs.DeliveryAgencies;
using PrettyWoman.Application.DTOs.ExpenseCategories;
using PrettyWoman.Application.DTOs.LoanOwners;
using PrettyWoman.Application.DTOs.PaymentTerminals;
using PrettyWoman.Application.DTOs.Sizes;
using PrettyWoman.Application.DTOs.Subcategories;
using PrettyWoman.Application.DTOs.Suppliers;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Mappings;

public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<CreateSizeDTO, Size>();
        CreateMap<UpdateSizeDTO, Size>();
        CreateMap<SizeGroup, SizeGroupDTO>();
        CreateMap<Size, SizeDTO>();

        CreateMap<CreateClientDTO, Client>();
        CreateMap<UpdateClientDTO, Client>();
        CreateMap<Client, ClientDTO>();

        CreateMap<CreateDeliveryAgencyDTO, DeliveryAgency>();
        CreateMap<UpdateDeliveryAgencyDTO, DeliveryAgency>();
        CreateMap<DeliveryAgency, DeliveryAgencyDTO>();

        CreateMap<CreatePaymentTerminalDTO, PaymentTerminal>();
        CreateMap<UpdatePaymentTerminalDTO, PaymentTerminal>();
        CreateMap<PaymentTerminal, PaymentTerminalDTO>();

        CreateMap<CreateExpenseCategoryDTO, ExpenseCategory>();
        CreateMap<UpdateExpenseCategoryDTO, ExpenseCategory>();
        CreateMap<ExpenseCategory, ExpenseCategoryDTO>();

        CreateMap<CreateLoanOwnerDTO, LoanOwner>();
        CreateMap<UpdateLoanOwnerDTO, LoanOwner>();
        CreateMap<LoanOwner, LoanOwnerDTO>();

        CreateMap<CreateCategoryDTO, Category>();
        CreateMap<UpdateCategoryDTO, Category>();
        CreateMap<Category, CategoryDTO>();

        CreateMap<CreateSubcategoryDTO, Subcategory>();
        CreateMap<UpdateSubcategoryDTO, Subcategory>();
        CreateMap<Subcategory, SubcategoryDTO>()
            .ForMember(
                destination => destination.CategoryName,
                options => options.MapFrom(source => source.Category != null ? source.Category.Name : null));

        CreateMap<CreateSupplierDTO, Supplier>();
        CreateMap<UpdateSupplierDTO, Supplier>();
        CreateMap<Supplier, SupplierDTO>();
    }
}
