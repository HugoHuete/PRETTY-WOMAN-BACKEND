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
        createDeliveryAgencyDTO.Name = createDeliveryAgencyDTO.Name.NormalizeRequired("Delivery agency name");
        createDeliveryAgencyDTO.PhoneNumber = createDeliveryAgencyDTO.PhoneNumber.NormalizeRequired("Delivery agency phone number");

        var exists = await _context.DeliveryAgencies
            .AnyAsync(deliveryAgency => deliveryAgency.Name.ToLower() == createDeliveryAgencyDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A delivery agency with that name already exists.");
        }

        var deliveryAgency = _mapper.Map<DeliveryAgency>(createDeliveryAgencyDTO);

        await _context.DeliveryAgencies.AddAsync(deliveryAgency);
        await _context.SaveChangesAsync();

        return deliveryAgency.Id;
    }

    public async Task UpdateAsync(int id, UpdateDeliveryAgencyDTO updateDeliveryAgencyDTO)
    {
        var deliveryAgency = await _context.DeliveryAgencies.FirstOrDefaultAsync(deliveryAgency => deliveryAgency.Id == id)
            ?? throw new AppNotFoundException($"Delivery agency with id '{id}' does not exist.");

        updateDeliveryAgencyDTO.Name = updateDeliveryAgencyDTO.Name.NormalizeRequired("Delivery agency name");
        updateDeliveryAgencyDTO.PhoneNumber = updateDeliveryAgencyDTO.PhoneNumber.NormalizeRequired("Delivery agency phone number");

        var exists = await _context.DeliveryAgencies
            .AnyAsync(deliveryAgency => deliveryAgency.Id != id && deliveryAgency.Name.ToLower() == updateDeliveryAgencyDTO.Name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("A delivery agency with that name already exists.");
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
            ?? throw new AppNotFoundException($"Delivery agency with id '{id}' does not exist.");

        return _mapper.Map<DeliveryAgencyDTO>(deliveryAgency);
    }
}
