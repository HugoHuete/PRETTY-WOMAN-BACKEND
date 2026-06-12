using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Suppliers;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class SupplierService(IApplicationDbContext context, IMapper mapper) : ISupplierService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateSupplierDTO createSupplierDTO)
    {
        createSupplierDTO.Name = createSupplierDTO.Name.NormalizeRequired("Nombre del proveedor");

        var exists = await _context.Suppliers
            .AnyAsync(s => s.Name.ToLower() == createSupplierDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe un proveedor con ese nombre.");
        }

        var supplier = _mapper.Map<Supplier>(createSupplierDTO);

        await _context.Suppliers.AddAsync(supplier);
        await _context.SaveChangesAsync();

        return supplier.Id;
    }

    public async Task UpdateAsync(int id, UpdateSupplierDTO updateSupplierDTO)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new AppNotFoundException($"El proveedor con id '{id}' no existe.");

        updateSupplierDTO.Name = updateSupplierDTO.Name.NormalizeRequired("Nombre del proveedor");

        var exists = await _context.Suppliers
            .AnyAsync(s => s.Id != id && s.Name.ToLower() == updateSupplierDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe un proveedor con ese nombre.");
        }

        _mapper.Map(updateSupplierDTO, supplier);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SupplierDTO>> GetAllAsync()
    {
        var suppliers = await _context.Suppliers.ToListAsync();

        return _mapper.Map<List<SupplierDTO>>(suppliers);
    }

    public async Task<SupplierDTO> GetByIdAsync(int id)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id)
            ?? throw new AppNotFoundException($"El proveedor con id '{id}' no existe.");

        return _mapper.Map<SupplierDTO>(supplier);
    }
}
