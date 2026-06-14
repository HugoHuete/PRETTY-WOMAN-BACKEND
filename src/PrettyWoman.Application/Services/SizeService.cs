using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Sizes;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class SizeService(IApplicationDbContext context, IMapper mapper) : ISizeService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateSizeDTO createSizeDTO)
    {
        createSizeDTO.Name = createSizeDTO.Name.NormalizeRequired("Nombre de la talla");
        await EnsureSizeGroupExistsAsync(createSizeDTO.SizeGroupId);

        var exists = await _context.Sizes
            .AnyAsync(size =>
                size.SizeGroupId == createSizeDTO.SizeGroupId &&
                size.Name.ToLower() == createSizeDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una talla con ese nombre en el grupo seleccionado.");
        }

        var size = _mapper.Map<Size>(createSizeDTO);

        await _context.Sizes.AddAsync(size);
        await _context.SaveChangesAsync();

        return size.Id;
    }

    public async Task UpdateAsync(int id, UpdateSizeDTO updateSizeDTO)
    {
        var size = await _context.Sizes.FirstOrDefaultAsync(size => size.Id == id)
            ?? throw new AppNotFoundException($"La talla con id '{id}' no existe.");

        updateSizeDTO.Name = updateSizeDTO.Name.NormalizeRequired("Nombre de la talla");
        await EnsureSizeGroupExistsAsync(updateSizeDTO.SizeGroupId);

        var exists = await _context.Sizes
            .AnyAsync(size =>
                size.Id != id &&
                size.SizeGroupId == updateSizeDTO.SizeGroupId &&
                size.Name.ToLower() == updateSizeDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una talla con ese nombre en el grupo seleccionado.");
        }

        _mapper.Map(updateSizeDTO, size);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SizeDTO>> GetAllAsync()
    {
        var sizes = await _context.Sizes
            .Include(size => size.SizeGroup)
            .OrderBy(size => new { size.SizeGroupId, size.DisplayOrder })
            .ToListAsync();

        return _mapper.Map<List<SizeDTO>>(sizes);
    }

    public async Task<SizeDTO> GetByIdAsync(int id)
    {
        var size = await _context.Sizes
            .Include(size => size.SizeGroup)
            .FirstOrDefaultAsync(size => size.Id == id)
            ?? throw new AppNotFoundException($"La talla con id '{id}' no existe.");

        return _mapper.Map<SizeDTO>(size);
    }

    private async Task EnsureSizeGroupExistsAsync(int sizeGroupId)
    {
        var exists = await _context.SizeGroups.AnyAsync(sizeGroup => sizeGroup.Id == sizeGroupId);

        if (!exists)
        {
            throw new AppNotFoundException($"El grupo de talla con id '{sizeGroupId}' no existe.");
        }
    }
}
