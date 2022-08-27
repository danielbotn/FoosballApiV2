using AutoMapper;
using FoosballApi.Dtos.DoubleGoals;
using FoosballApi.Dtos.DoubleMatches;
using FoosballApi.Dtos.Goals;
using FoosballApi.Dtos.Leagues;
using FoosballApi.Dtos.Matches;
using FoosballApi.Dtos.Organisations;
using FoosballApi.Dtos.Users;
using FoosballApi.Models;
using FoosballApi.Models.Goals;
using FoosballApi.Models.Leagues;
using FoosballApi.Models.Matches;
using FoosballApi.Models.Organisations;
using FoosballApi.Models.Users;

namespace FoosballApi.Profiles
{
    public class UsersProfile : Profile
    {
        public UsersProfile()
        {
            CreateMap<User, UserReadDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Created_at));

            CreateMap<UserUpdateDto, User>();
            CreateMap<User, UserUpdateDto>();
            CreateMap<UserCreateDto, User>();
            CreateMap<UserStats, UserStatsReadDto>();
            CreateMap<UserStatsReadDto, UserStats>();

            CreateMap<OrganisationModel, OrganisationReadDto>();
            CreateMap<OrganisationUpdateDto, OrganisationModel>();
            CreateMap<OrganisationModel, OrganisationUpdateDto>();

            CreateMap<LeagueModel, LeagueReadDto>();
            CreateMap<LeagueReadDto, LeagueModel>();

            CreateMap<LeaguePlayersModel, LeaguePlayersReadDto>();
            CreateMap<LeaguePlayersReadDto, LeaguePlayersModel>();

            CreateMap<LeaguePlayersJoinModel, LeaguePlayersReadDto>();
            CreateMap<LeaguePlayersReadDto, LeaguePlayersJoinModel>();

            CreateMap<OrganisationReadDto, OrganisationModelCreate>();
            CreateMap<OrganisationModelCreate, OrganisationReadDto>();

            CreateMap<LeagueUpdateDto, LeagueModel>();
            CreateMap<LeagueModel, LeagueUpdateDto>();

            CreateMap<FreehandMatchModel, FreehandMatchesReadDto>();
            CreateMap<FreehandMatchesReadDto, FreehandMatchModel>();

            CreateMap<FreehandMatchModel, FreehandMatchCreateResultDto>();
            CreateMap<FreehandMatchCreateResultDto, FreehandMatchModel>();

            CreateMap<FreehandMatchModelExtended, FreehandMatchesReadDto>();
            CreateMap<FreehandMatchesReadDto, FreehandMatchModelExtended>();

            CreateMap<FreehandMatchCreateDto, FreehandMatchModel>();
            CreateMap<FreehandMatchModel, FreehandMatchCreateDto>();

            CreateMap<FreehandMatchModel, FreehandMatchUpdateDto>();
            CreateMap<FreehandMatchUpdateDto, FreehandMatchModel>();

            CreateMap<FreehandGoalModel, FreehandGoalReadDto>();
            CreateMap<FreehandGoalReadDto, FreehandGoalModel>();

            CreateMap<FreehandGoalModel, FreehandGoalCreateResultDto>();
            CreateMap<FreehandGoalCreateResultDto, FreehandGoalModel>();

            CreateMap<FreehandGoalModelExtended, FreehandGoalReadDto>();
            CreateMap<FreehandGoalReadDto, FreehandGoalModelExtended>();

            CreateMap<FreehandDoubleMatchReadDto, FreehandDoubleMatchModel>();
            CreateMap<FreehandDoubleMatchModel, FreehandDoubleMatchReadDto>();

            CreateMap<FreehandDoubleMatchResponseDto, FreehandDoubleMatchModel>();
            CreateMap<FreehandDoubleMatchModel, FreehandDoubleMatchResponseDto>();

            CreateMap<FreehandDoubleMatchReadDto, FreehandDoubleMatchModelExtended>();
            CreateMap<FreehandDoubleMatchModelExtended, FreehandDoubleMatchReadDto>();

            CreateMap<FreehandDoubleGoalReadDto, FreehandDoubleGoalModel>();
            CreateMap<FreehandDoubleGoalModel, FreehandDoubleGoalReadDto>();

            CreateMap<MatchReadDto, Match>();
            CreateMap<Match, MatchReadDto>();

        }

    }
}