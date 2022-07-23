using AutoMapper;
using FoosballApi.Dtos.DoubleMatches;
using FoosballApi.Models.Matches;

namespace FoosballApi.Profiles
{
    public class FreehandDoubleMatchProfile : Profile
    {
        public FreehandDoubleMatchProfile()
        {
            CreateMap<FreehandDoubleMatchModel, FreehandDoubleMatchUpdateDto>();
            CreateMap<FreehandDoubleMatchUpdateDto, FreehandDoubleMatchModel>();
        }
    }
}