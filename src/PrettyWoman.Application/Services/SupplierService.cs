using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Suppliers;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class SupplierService(IApplicationDbContext context, IMapper mapper) : ISupplierService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateSupplierDTO createSupplierDTO)
    {
        var normalizedName = createSupplierDTO.Name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Supplier name is required.");
        }

        var exists = await _context.Suppliers
            .AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            throw new InvalidOperationException("A supplier with that name already exists.");
        }

        createSupplierDTO.Name = normalizedName;

        var supplier = _mapper.Map<Supplier>(createSupplierDTO);

        await _context.Suppliers.AddAsync(supplier);
        await _context.SaveChangesAsync();

        return supplier.Id;
    }
}
