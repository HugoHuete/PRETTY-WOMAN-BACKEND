using AutoMapper;
using PrettyWoman.Application.DTOs.Categories;
using PrettyWoman.Application.DTOs.Subcategories;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Mappings;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<CreateCategoryDTO, Category>();
        CreateMap<UpdateCategoryDTO, Category>();
        CreateMap<Category, CategoryDTO>();

        CreateMap<CreateSubcategoryDTO, Subcategory>();
        CreateMap<UpdateSubcategoryDTO, Subcategory>();
        CreateMap<Subcategory, SubcategoryDTO>()
            .ForMember(
                destination => destination.CategoryName,
                options => options.MapFrom(source => source.Category != null ? source.Category.Name : null));
    }
}
