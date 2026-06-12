using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Subcategories;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class SubcategoryService(IApplicationDbContext context, IMapper mapper) : ISubcategoryService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateSubcategoryDTO createSubcategoryDTO)
    {
        var normalizedName = createSubcategoryDTO.Name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Subcategory name is required.");
        }

        await EnsureCategoryExistsAsync(createSubcategoryDTO.CategoryId);

        var exists = await _context.Subcategories
            .AnyAsync(subcategory => subcategory.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A subcategory with that name already exists.");
        }

        createSubcategoryDTO.Name = normalizedName;

        var subcategory = _mapper.Map<Subcategory>(createSubcategoryDTO);

        await _context.Subcategories.AddAsync(subcategory);
        await _context.SaveChangesAsync();

        return subcategory.Id;
    }

    public async Task UpdateAsync(int id, UpdateSubcategoryDTO updateSubcategoryDTO)
    {
        var subcategory = await _context.Subcategories.FirstOrDefaultAsync(subcategory => subcategory.Id == id)
            ?? throw new AppNotFoundException($"Subcategory with id '{id}' does not exist.");

        var normalizedName = updateSubcategoryDTO.Name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Subcategory name is required.");
        }

        await EnsureCategoryExistsAsync(updateSubcategoryDTO.CategoryId);

        var exists = await _context.Subcategories
            .AnyAsync(subcategory => subcategory.Id != id && subcategory.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A subcategory with that name already exists.");
        }

        updateSubcategoryDTO.Name = normalizedName;

        _mapper.Map(updateSubcategoryDTO, subcategory);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SubcategoryDTO>> GetAllAsync(int? categoryId = null)
    {
        if (categoryId.HasValue)
        {
            await EnsureCategoryExistsAsync(categoryId.Value);
        }

        var subcategories = await _context.Subcategories
            .Include(subcategory => subcategory.Category)
            .Where(subcategory => !categoryId.HasValue || subcategory.CategoryId == categoryId.Value)
            .ToListAsync();

        return _mapper.Map<List<SubcategoryDTO>>(subcategories);
    }

    public async Task<SubcategoryDTO> GetByIdAsync(int id)
    {
        var subcategory = await _context.Subcategories
            .Include(subcategory => subcategory.Category)
            .FirstOrDefaultAsync(subcategory => subcategory.Id == id)
            ?? throw new AppNotFoundException($"Subcategory with id '{id}' does not exist.");

        return _mapper.Map<SubcategoryDTO>(subcategory);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        var exists = await _context.Categories.AnyAsync(category => category.Id == categoryId);

        if (!exists)
        {
            throw new AppNotFoundException($"Category with id '{categoryId}' does not exist.");
        }
    }
}
