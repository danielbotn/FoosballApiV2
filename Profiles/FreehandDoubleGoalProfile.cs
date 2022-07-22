using AutoMapper;
using FoosballApi.Dtos.DoubleGoals;
using FoosballApi.Models.Goals;

namespace FoosballApi.Profiles
{
    public class FreehandDoubleGoalProfile : Profile
    {
        public FreehandDoubleGoalProfile()
        {
            CreateMap<FreehandDoubleGoalUpdateDto, FreehandDoubleGoalModel>();
            CreateMap<FreehandDoubleGoalModel, FreehandDoubleGoalUpdateDto>();
        }
    }
}