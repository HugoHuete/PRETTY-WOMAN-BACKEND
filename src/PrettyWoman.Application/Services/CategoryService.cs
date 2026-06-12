using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        var normalizedName = createCategoryDTO.Name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Category name is required.");
        }

        var exists = await _context.Categories
            .AnyAsync(category => category.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A category with that name already exists.");
        }

        createCategoryDTO.Name = normalizedName;

        var category = _mapper.Map<Category>(createCategoryDTO);

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        return category.Id;
    }

    public async Task UpdateAsync(int id, UpdateCategoryDTO updateCategoryDTO)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == id)
            ?? throw new AppNotFoundException($"Category with id '{id}' does not exist.");

        var normalizedName = updateCategoryDTO.Name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Category name is required.");
        }

        var exists = await _context.Categories
            .AnyAsync(category => category.Id != id && category.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A category with that name already exists.");
        }

        updateCategoryDTO.Name = normalizedName;

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
