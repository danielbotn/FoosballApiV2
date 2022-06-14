using AutoMapper;
using FoosballApi.Dtos.DoubleLeaguePlayers;
using FoosballApi.Models.DoubleLeaguePlayers;

namespace FoosballApi.Profiles
{
    public class DoubleLeaguePlayerProfile : Profile
    {
        public DoubleLeaguePlayerProfile()
        {
            CreateMap<DoubleLeaguePlayerModelDapper, DoubleLeaguePlayerReadDto>();
            CreateMap<DoubleLeaguePlayerReadDto, DoubleLeaguePlayerModelDapper>();
        }
    }
}
