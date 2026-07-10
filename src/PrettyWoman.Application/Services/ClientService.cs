using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Clients;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public class ClientService(IApplicationDbContext context, IMapper mapper) : IClientService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateClientDTO createClientDTO)
    {
        NormalizeClientFields(createClientDTO);
        await EnsureUniqueContactDataAsync(createClientDTO.PhoneNumber, createClientDTO.InstagramUser, createClientDTO.MessengerUser);

        var client = _mapper.Map<Client>(createClientDTO);
        client.CreatedAt = DateTime.UtcNow;
        client.IsBlocked = false;
        client.BlockedReason = null;

        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        return client.Id;
    }

    public async Task UpdateAsync(int id, UpdateClientDTO updateClientDTO)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(client => client.Id == id)
            ?? throw new AppNotFoundException($"El cliente con id '{id}' no existe.");

        NormalizeClientFields(updateClientDTO);
        await EnsureUniqueContactDataAsync(updateClientDTO.PhoneNumber, updateClientDTO.InstagramUser, updateClientDTO.MessengerUser, id);

        _mapper.Map(updateClientDTO, client);

        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResult<ClientDTO>> GetAllAsync(ClientQueryDTO query)
    {
        NormalizePagination(query);

        var clientsQuery = ApplyFilters(_context.Clients.AsNoTracking().AsQueryable(), query);
        var totalCount = await clientsQuery.CountAsync();
        var clients = await clientsQuery
            .OrderBy(client => client.Name)
            .ThenBy(client => client.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PaginatedResult<ClientDTO>
        {
            Items = _mapper.Map<List<ClientDTO>>(clients),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ClientDTO> GetByIdAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(client => client.Id == id)
            ?? throw new AppNotFoundException($"El cliente con id '{id}' no existe.");

        return _mapper.Map<ClientDTO>(client);
    }

    public async Task BlockAsync(int id, BlockClientDTO blockClientDTO)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(client => client.Id == id)
            ?? throw new AppNotFoundException($"El cliente con id '{id}' no existe.");

        blockClientDTO.BlockedReason = blockClientDTO.BlockedReason.NormalizeRequired("Motivo de bloqueo");

        client.IsBlocked = true;
        client.BlockedReason = blockClientDTO.BlockedReason;

        await _context.SaveChangesAsync();
    }

    public async Task UnblockAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(client => client.Id == id)
            ?? throw new AppNotFoundException($"El cliente con id '{id}' no existe.");

        client.IsBlocked = false;
        client.BlockedReason = null;

        await _context.SaveChangesAsync();
    }

    private static void NormalizeClientFields(CreateClientDTO clientDTO)
    {
        clientDTO.Name = clientDTO.Name.NormalizeRequired("Nombre del cliente");
        clientDTO.PhoneNumber = clientDTO.PhoneNumber.NormalizeOptional();
        clientDTO.InstagramUser = clientDTO.InstagramUser.NormalizeOptional()?.ToLower();
        clientDTO.MessengerUser = clientDTO.MessengerUser.NormalizeOptional()?.ToLower();
        clientDTO.Address = clientDTO.Address.NormalizeOptional();
        clientDTO.Comments = clientDTO.Comments.NormalizeOptional();
    }

    private static IQueryable<Client> ApplyFilters(IQueryable<Client> clientsQuery, ClientQueryDTO query)
    {
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLower();
            clientsQuery = clientsQuery.Where(client => client.Name.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
        {
            var phoneNumber = query.PhoneNumber.Trim().ToLower();
            clientsQuery = clientsQuery.Where(client => client.PhoneNumber != null && client.PhoneNumber.ToLower().Contains(phoneNumber));
        }

        if (!string.IsNullOrWhiteSpace(query.InstagramUser))
        {
            var instagramUser = query.InstagramUser.Trim().ToLower();
            clientsQuery = clientsQuery.Where(client => client.InstagramUser != null && client.InstagramUser.ToLower().Contains(instagramUser));
        }

        if (!string.IsNullOrWhiteSpace(query.MessengerUser))
        {
            var messengerUser = query.MessengerUser.Trim().ToLower();
            clientsQuery = clientsQuery.Where(client => client.MessengerUser != null && client.MessengerUser.ToLower().Contains(messengerUser));
        }

        return clientsQuery;
    }

    private static void NormalizePagination(ClientQueryDTO query)
    {
        query.Page = Math.Max(query.Page, 1);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);
    }
    private async Task  EnsureUniqueContactDataAsync(string? phoneNumber, string? instagramUser, string? messengerUser, int? clientId = null)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phoneExists = await _context.Clients.AnyAsync(client =>
                client.Id != clientId &&
                client.PhoneNumber != null &&
                client.PhoneNumber.ToLower() == phoneNumber.ToLower());

            if (phoneExists)
            {
                throw new AppBadRequestException("Ya existe un cliente con ese número de teléfono.");
            }
        }

        if (!string.IsNullOrWhiteSpace(instagramUser))
        {
            var instagramExists = await _context.Clients.AnyAsync(client =>
                client.Id != clientId &&
                client.InstagramUser != null &&
                client.InstagramUser.ToLower() == instagramUser.ToLower());

            if (instagramExists)
            {
                throw new AppBadRequestException("Ya existe un cliente con ese usuario de Instagram.");
            }
        }

        if (!string.IsNullOrWhiteSpace(messengerUser))
        {
            var instagramExists = await _context.Clients.AnyAsync(client =>
                client.Id != clientId &&
                client.MessengerUser != null &&
                client.MessengerUser.ToLower() == messengerUser.ToLower());

            if (instagramExists)
            {
                throw new AppBadRequestException("Ya existe un cliente con ese usuario de Messenger.");
            }
        }
    }
}
