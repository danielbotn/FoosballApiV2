using AutoMapper;
using FoosballApi.Dtos.DoubleLeagueMatches;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.Other;

namespace FoosballApi.Profiles
{
    public class DoubleLeagueMatchProfile : Profile
    {
        public DoubleLeagueMatchProfile()
        {
            CreateMap<AllMatchesModelReadDto, AllMatchesModel>();
            CreateMap<AllMatchesModel, AllMatchesModelReadDto>();

            CreateMap<DoubleLeagueStandingsReadDto, DoubleLeagueStandingsQuery>();
            CreateMap<DoubleLeagueStandingsQuery, DoubleLeagueStandingsReadDto>();

            CreateMap<DoubleLeagueMatchModel, DoubleLeagueMatchUpdateDto>();
            CreateMap<DoubleLeagueMatchUpdateDto, DoubleLeagueMatchModel>();
        }
    }
}