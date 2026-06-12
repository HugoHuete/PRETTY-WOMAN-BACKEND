using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Categories;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class CategoryService(IApplicationDbContext context, IMapper mapper) : ICategoryService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateCategoryDTO createCategoryDTO)
    {
        createCategoryDTO.Name = createCategoryDTO.Name.NormalizeRequired("Category name");

        var exists = await _context.Categories
            .AnyAsync(category => category.Name.ToLower() == createCategoryDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A category with that name already exists.");
        }

        var category = _mapper.Map<Category>(createCategoryDTO);

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        return category.Id;
    }

    public async Task UpdateAsync(int id, UpdateCategoryDTO updateCategoryDTO)
    {
        updateCategoryDTO.Name = updateCategoryDTO.Name.NormalizeRequired("Category name");
        
        var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == id)
            ?? throw new AppNotFoundException($"Category with id '{id}' does not exist.");


        var exists = await _context.Categories
            .AnyAsync(category => category.Id != id && category.Name.ToLower() == updateCategoryDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A category with that name already exists.");
        }

        _mapper.Map(updateCategoryDTO, category);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<CategoryDTO>> GetAllAsync()
    {
        var categories = await _context.Categories.ToListAsync();

        return _mapper.Map<List<CategoryDTO>>(categories);
    }

    public async Task<CategoryDTO> GetByIdAsync(int id)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == id)
            ?? throw new AppNotFoundException($"Category with id '{id}' does not exist.");

        return _mapper.Map<CategoryDTO>(category);
    }
}
