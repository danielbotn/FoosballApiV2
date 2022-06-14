using AutoMapper;
using FoosballApi.Dtos.DoubleLeagueGoals;
using FoosballApi.Models.DoubleLeagueGoals;

namespace FoosballApi.Profiles
{
    public class DoubleLeagueGoalProfile : Profile
    {
        public DoubleLeagueGoalProfile()
        {
            CreateMap<DoubleLeagueGoalReadDto, DoubleLeagueGoalDapper>();
            CreateMap<DoubleLeagueGoalDapper, DoubleLeagueGoalReadDto>();
            CreateMap<DoubleLeagueGoalReadDto, DoubleLeagueGoalModel>();
            CreateMap<DoubleLeagueGoalModel, DoubleLeagueGoalReadDto>();
            CreateMap<DoubleLeagueGoalReadDto, DoubleLeagueGoalExtended>();
            CreateMap<DoubleLeagueGoalExtended, DoubleLeagueGoalReadDto>();
        }
    }
}