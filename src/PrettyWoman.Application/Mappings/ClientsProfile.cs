using AutoMapper;
using PrettyWoman.Application.DTOs.Clients;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Mappings;

public class ClientsProfile : Profile
{
    public ClientsProfile()
    {
        CreateMap<CreateClientDTO, Client>();
        CreateMap<UpdateClientDTO, Client>();
        CreateMap<Client, ClientDTO>();
    }
}
