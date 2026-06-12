using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.ExpenseCategories;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class ExpenseCategoryService(IApplicationDbContext context, IMapper mapper) : IExpenseCategoryService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateExpenseCategoryDTO createExpenseCategoryDTO)
    {
        createExpenseCategoryDTO.Name = createExpenseCategoryDTO.Name.NormalizeRequired("Nombre de la categoría de gasto");

        var exists = await _context.ExpenseCategories
            .AnyAsync(expenseCategory => expenseCategory.Name.ToLower() == createExpenseCategoryDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una categoría de gasto con ese nombre.");
        }

        var expenseCategory = _mapper.Map<ExpenseCategory>(createExpenseCategoryDTO);

        await _context.ExpenseCategories.AddAsync(expenseCategory);
        await _context.SaveChangesAsync();

        return expenseCategory.Id;
    }

    public async Task UpdateAsync(int id, UpdateExpenseCategoryDTO updateExpenseCategoryDTO)
    {
        var expenseCategory = await _context.ExpenseCategories.FirstOrDefaultAsync(expenseCategory => expenseCategory.Id == id)
            ?? throw new AppNotFoundException($"La categoría de gasto con id '{id}' no existe.");

        updateExpenseCategoryDTO.Name = updateExpenseCategoryDTO.Name.NormalizeRequired("Nombre de la categoría de gasto");

        var exists = await _context.ExpenseCategories
            .AnyAsync(expenseCategory => expenseCategory.Id != id && expenseCategory.Name.ToLower() == updateExpenseCategoryDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una categoría de gasto con ese nombre.");
        }

        _mapper.Map(updateExpenseCategoryDTO, expenseCategory);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ExpenseCategoryDTO>> GetAllAsync()
    {
        var expenseCategories = await _context.ExpenseCategories
            .OrderBy(expenseCategory => expenseCategory.Name)
            .ToListAsync();

        return _mapper.Map<List<ExpenseCategoryDTO>>(expenseCategories);
    }

    public async Task<ExpenseCategoryDTO> GetByIdAsync(int id)
    {
        var expenseCategory = await _context.ExpenseCategories.FirstOrDefaultAsync(expenseCategory => expenseCategory.Id == id)
            ?? throw new AppNotFoundException($"La categoría de gasto con id '{id}' no existe.");

        return _mapper.Map<ExpenseCategoryDTO>(expenseCategory);
    }
}
