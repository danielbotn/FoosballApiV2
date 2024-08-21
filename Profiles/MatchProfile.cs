using AutoMapper;
using FoosballApi.Enums;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.Matches;
using FoosballApi.Models.Other;

namespace FoosballApi.Profiles
{
    public class MatchProfile : Profile
    {
        public MatchProfile()
        {
            CreateMap<FreehandMatchModelExtended, Match>()
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.FreehandMatch)) // Set TypeOfMatch to FreehandMatch
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => "FreehandMatch")) // Set TypeOfMatchName to "FreehandMatch"
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.PlayerOneId))
                .ForMember(dest => dest.TeamMateId, opt => opt.Ignore()) // Assuming TeamMateId comes from elsewhere
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.PlayerTwoId))
                .ForMember(dest => dest.OpponentTwoId, opt => opt.Ignore()) // Assuming OpponentTwoId comes from elsewhere

                // Mapping opponent details
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.PlayerTwoFirstName))
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.PlayerTwoLastName))
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.PlayerTwoPhotoUrl))

                // Ignoring opponent two details since it's not available in FreehandMatchModelExtended
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.Ignore())

                // Mapping teammate details (assuming it comes from PlayerThree or elsewhere)
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.Ignore()) // Assuming these come from other sources
                .ForMember(dest => dest.TeamMateLastName, opt => opt.Ignore())
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.Ignore())

                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.PlayerOneScore))
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.PlayerTwoScore))
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime));

            CreateMap<FreehandDoubleMatchModel, Match>()
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.DoubleFreehandMatch)) // Set TypeOfMatch to DoubleFreehandMatch
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => "DoubleFreehandMatch")) // Set TypeOfMatchName to "DoubleFreehandMatch"

                // Mapping player and team details
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.PlayerOneTeamA)) // Mapping PlayerOneTeamA as UserId
                .ForMember(dest => dest.TeamMateId, opt => opt.MapFrom(src => src.PlayerTwoTeamA)) // Mapping PlayerTwoTeamA as TeamMateId
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.PlayerOneTeamB)) // Mapping PlayerOneTeamB as OpponentId
                .ForMember(dest => dest.OpponentTwoId, opt => opt.MapFrom(src => src.PlayerTwoTeamB)) // Mapping PlayerTwoTeamB as OpponentTwoId

                // Scores and timing
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.TeamAScore.HasValue ? src.TeamAScore.Value : 0)) // TeamA score as UserScore
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.TeamBScore.HasValue ? src.TeamBScore.Value : 0)) // TeamB score as Opponent score
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime.HasValue ? src.StartTime.Value : DateTime.MinValue)) // Map StartTime to DateOfGame
                .ForMember(dest => dest.LeagueId, opt => opt.Ignore()); // Assuming LeagueId comes from elsewhere

            CreateMap<SingleLeagueMatchesQuery, Match>()
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.SingleLeagueMatch)) // Assuming it's a single league match
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => ETypeOfMatch.SingleLeagueMatch.ToString())) // Convert enum to string
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.PlayerOne))
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.PlayerTwo))
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.PlayerOneScore ?? 0))
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.PlayerTwoScore ?? 0))
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime ?? DateTime.MinValue))
                .ForMember(dest => dest.LeagueId, opt => opt.MapFrom(src => src.LeagueId))
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.PlayerTwoFirstName))
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.PlayerTwoLastName))
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.PlayerTwoPhotoUrl))
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.Ignore()) // No team mate in single matches
                .ForMember(dest => dest.TeamMateLastName, opt => opt.Ignore())
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.Ignore()) // No second opponent in single matches
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.Ignore());

            CreateMap<AllMatchesModel, Match>()
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.DoubleLeagueMatch)) // Assuming this is a double league match
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => ETypeOfMatch.DoubleLeagueMatch.ToString())) // Convert enum to string
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.TeamOne[0].UserId)) // Assuming the first player of TeamOne as the UserId
                .ForMember(dest => dest.TeamMateId, opt => opt.MapFrom(src => src.TeamOne.Length > 1 ? (int?)src.TeamOne[1].UserId : null)) // If TeamOne has a second player
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.TeamTwo[0].UserId)) // Assuming the first player of TeamTwo as the OpponentId
                .ForMember(dest => dest.OpponentTwoId, opt => opt.MapFrom(src => src.TeamTwo.Length > 1 ? (int?)src.TeamTwo[1].UserId : null)) // If TeamTwo has a second player
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.TeamTwo[0].FirstName))
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.TeamTwo[0].LastName))
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.TeamTwo[0].PhotoUrl))
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.MapFrom(src => src.TeamTwo.Length > 1 ? src.TeamTwo[1].FirstName : null))
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.MapFrom(src => src.TeamTwo.Length > 1 ? src.TeamTwo[1].LastName : null))
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.MapFrom(src => src.TeamTwo.Length > 1 ? src.TeamTwo[1].PhotoUrl : null))
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.MapFrom(src => src.TeamOne.Length > 1 ? src.TeamOne[1].FirstName : null))
                .ForMember(dest => dest.TeamMateLastName, opt => opt.MapFrom(src => src.TeamOne.Length > 1 ? src.TeamOne[1].LastName : null))
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.MapFrom(src => src.TeamOne.Length > 1 ? src.TeamOne[1].PhotoUrl : null))
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.TeamOneScore))
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.TeamTwoScore))
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime ?? DateTime.MinValue))
                .ForMember(dest => dest.LeagueId, opt => opt.MapFrom(src => src.LeagueId));
        }
    }
}
