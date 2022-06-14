using AutoMapper;
using FoosballApi.Dtos.SingleLeagueMatches;
using FoosballApi.Models.Matches;
using FoosballApi.Models.Other;

namespace FoosballApi.Profiles
{
    public class SingleLeagueMatchProfile : Profile
    {
        public SingleLeagueMatchProfile()
        {
            CreateMap<SingleLeagueMatchModel, SingleLeagueMatchReadDto>();
            CreateMap<SingleLeagueMatchReadDto, SingleLeagueMatchModel>();
            CreateMap<SingleLeagueMatchModelExtended, SingleLeagueMatchReadDto>();
            CreateMap<SingleLeagueMatchReadDto, SingleLeagueMatchModelExtended>();
            CreateMap<SingleLeagueMatchModel, SingleLeagueMatchUpdateDto>();
            CreateMap<SingleLeagueMatchUpdateDto, SingleLeagueMatchModel>();
            CreateMap<SingleLeagueStandingsReadDto, SingleLeagueStandingsQuery>();
            CreateMap<SingleLeagueStandingsQuery, SingleLeagueStandingsReadDto>();
        }
    }
}