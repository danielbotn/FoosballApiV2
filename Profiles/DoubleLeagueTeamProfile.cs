using AutoMapper;
using FoosballApi.Dtos.DoubleLeagueTeams;
using FoosballApi.Models.DoubleLeagueTeams;

namespace FoosballApi.Profiles
{
    public class DoubleLeagueTeamProfile : Profile
    {
        public DoubleLeagueTeamProfile()
        {
            CreateMap<DoubleLeagueTeamModel, DoubleLeagueTeamReadDto>();
            CreateMap<DoubleLeagueTeamReadDto, DoubleLeagueTeamModel>();
        }
    }
}