using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.DeliveryAgencies;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class DeliveryAgencyService(IApplicationDbContext context, IMapper mapper) : IDeliveryAgencyService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateDeliveryAgencyDTO createDeliveryAgencyDTO)
    {
        createDeliveryAgencyDTO.Name = createDeliveryAgencyDTO.Name.NormalizeRequired("Nombre de la agencia de envío");
        createDeliveryAgencyDTO.PhoneNumber = createDeliveryAgencyDTO.PhoneNumber.NormalizeRequired("Teléfono de la agencia de envío");

        var exists = await _context.DeliveryAgencies
            .AnyAsync(deliveryAgency => deliveryAgency.Name.ToLower() == createDeliveryAgencyDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una agencia de envío con ese nombre.");
        }

        var deliveryAgency = _mapper.Map<DeliveryAgency>(createDeliveryAgencyDTO);

        await _context.DeliveryAgencies.AddAsync(deliveryAgency);
        await _context.SaveChangesAsync();

        return deliveryAgency.Id;
    }

    public async Task UpdateAsync(int id, UpdateDeliveryAgencyDTO updateDeliveryAgencyDTO)
    {
        var deliveryAgency = await _context.DeliveryAgencies.FirstOrDefaultAsync(deliveryAgency => deliveryAgency.Id == id)
            ?? throw new AppNotFoundException($"La agencia de envío con id '{id}' no existe.");

        updateDeliveryAgencyDTO.Name = updateDeliveryAgencyDTO.Name.NormalizeRequired("Nombre de la agencia de envío");
        updateDeliveryAgencyDTO.PhoneNumber = updateDeliveryAgencyDTO.PhoneNumber.NormalizeRequired("Teléfono de la agencia de envío");

        var exists = await _context.DeliveryAgencies
            .AnyAsync(deliveryAgency => deliveryAgency.Id != id && deliveryAgency.Name.ToLower() == updateDeliveryAgencyDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una agencia de envío con ese nombre.");
        }

        _mapper.Map(updateDeliveryAgencyDTO, deliveryAgency);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DeliveryAgencyDTO>> GetAllAsync()
    {
        var deliveryAgencies = await _context.DeliveryAgencies
            .OrderBy(deliveryAgency => deliveryAgency.Name)
            .ToListAsync();

        return _mapper.Map<List<DeliveryAgencyDTO>>(deliveryAgencies);
    }

    public async Task<DeliveryAgencyDTO> GetByIdAsync(int id)
    {
        var deliveryAgency = await _context.DeliveryAgencies.FirstOrDefaultAsync(deliveryAgency => deliveryAgency.Id == id)
            ?? throw new AppNotFoundException($"La agencia de envío con id '{id}' no existe.");

        return _mapper.Map<DeliveryAgencyDTO>(deliveryAgency);
    }
}
