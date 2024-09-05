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
                
                // Mapping user (PlayerOne) details
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.PlayerOneId))
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.PlayerOneFirstName))  // Mapping PlayerOne's First Name
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.PlayerOneLastName))    // Mapping PlayerOne's Last Name
                .ForMember(dest => dest.UserPhotoUrl, opt => opt.MapFrom(src => src.PlayerOnePhotoUrl))    // Mapping PlayerOne's Photo URL

                // Mapping teammate details (ignoring here as it's irrelevant for FreehandMatch)
                .ForMember(dest => dest.TeamMateId, opt => opt.Ignore()) // Assuming TeamMateId comes from elsewhere
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.Ignore())
                .ForMember(dest => dest.TeamMateLastName, opt => opt.Ignore())
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.Ignore())

                // Mapping match details
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.Id))
    
                // Mapping opponent (PlayerTwo) details
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.PlayerTwoId))
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.PlayerTwoFirstName))
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.PlayerTwoLastName))
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.PlayerTwoPhotoUrl))

                // Ignoring opponent two details since it's not available in FreehandMatchModelExtended
                .ForMember(dest => dest.OpponentTwoId, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.Ignore())

                // Mapping scores
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.PlayerOneScore))
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.PlayerTwoScore))
                
                // Mapping date of game
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime));


            CreateMap<FreehandDoubleMatchModel, Match>()
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.DoubleFreehandMatch)) // Set TypeOfMatch to DoubleFreehandMatch
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => "DoubleFreehandMatch")) // Set TypeOfMatchName to "DoubleFreehandMatch"

                // Mapping player and team details
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.PlayerOneTeamA)) // Mapping PlayerOneTeamA as UserId
                .ForMember(dest => dest.TeamMateId, opt => opt.MapFrom(src => src.PlayerTwoTeamA)) // Mapping PlayerTwoTeamA as TeamMateId
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.PlayerOneTeamB)) // Mapping PlayerOneTeamB as OpponentId
                .ForMember(dest => dest.OpponentTwoId, opt => opt.MapFrom(src => src.PlayerTwoTeamB)) // Mapping PlayerTwoTeamB as OpponentTwoId

                // Mapping player details (Team A - User and Teammate)
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.UserFirstName)) // Mapping UserFirstName
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.UserLastName)) // Mapping UserLastName
                .ForMember(dest => dest.UserPhotoUrl, opt => opt.MapFrom(src => src.UserPhotoUrl)) // Mapping UserPhotoUrl
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.MapFrom(src => src.TeamMateFirstName)) // Mapping PlayerTwoTeamA's first name
                .ForMember(dest => dest.TeamMateLastName, opt => opt.MapFrom(src => src.TeamMateLastName)) // Mapping PlayerTwoTeamA's last name
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.MapFrom(src => src.TeamMatePhotoUrl)) // Mapping PlayerTwoTeamA's photo URL

                // Mapping opponent details (Team B)
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.OpponentOneFirstName)) // Mapping PlayerOneTeamB's first name
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.OpponentOneLastName)) // Mapping PlayerOneTeamB's last name
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.OpponentOnePhotoUrl)) // Mapping PlayerOneTeamB's photo URL
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.MapFrom(src => src.OpponentTwoFirstName)) // Mapping PlayerTwoTeamB's first name
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.MapFrom(src => src.OpponentTwoLastName)) // Mapping PlayerTwoTeamB's last name
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.MapFrom(src => src.OpponentTwoPhotoUrl)) // Mapping PlayerTwoTeamB's photo URL

                // Scores and timing
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.TeamAScore.HasValue ? src.TeamAScore.Value : 0)) // TeamA score as UserScore
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.TeamBScore.HasValue ? src.TeamBScore.Value : 0)) // TeamB score as Opponent score
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime.HasValue ? src.StartTime.Value : DateTime.MinValue)) // Map StartTime to DateOfGame
                .ForMember(dest => dest.LeagueId, opt => opt.Ignore()); // Assuming LeagueId comes from elsewhere

            CreateMap<SingleLeagueMatchesQuery, Match>()
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.SingleLeagueMatch)) // Assuming it's a single league match
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => ETypeOfMatch.SingleLeagueMatch.ToString())) // Convert enum to string
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.PlayerOne)) // Map PlayerOne as UserId
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.PlayerTwo)) // Map PlayerTwo as OpponentId
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.Id)) // Map Id to MatchId
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.PlayerOneScore ?? 0)) // Map PlayerOneScore, default to 0 if null
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.PlayerTwoScore ?? 0)) // Map PlayerTwoScore, default to 0 if null
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime ?? DateTime.MinValue)) // Map StartTime, default to DateTime.MinValue if null
                .ForMember(dest => dest.LeagueId, opt => opt.MapFrom(src => src.LeagueId)) // Map LeagueId

                // Mapping opponent details (Player Two)
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.PlayerTwoFirstName)) // Map PlayerTwoFirstName
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.PlayerTwoLastName)) // Map PlayerTwoLastName
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.PlayerTwoPhotoUrl)) // Map PlayerTwoPhotoUrl

                // Mapping player details (Player One)
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.PlayerOneFirstName)) // Map PlayerOneFirstName
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.PlayerOneLastName)) // Map PlayerOneLastName
                .ForMember(dest => dest.UserPhotoUrl, opt => opt.MapFrom(src => src.PlayerOnePhotoUrl)) // Map PlayerOnePhotoUrl

                // Ignoring fields not relevant for single matches
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.Ignore()) // No team mate in single matches
                .ForMember(dest => dest.TeamMateLastName, opt => opt.Ignore())
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.Ignore()) // No second opponent in single matches
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.Ignore())
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.Ignore());


            CreateMap<AllMatchesModel, Match>()
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.DoubleLeagueMatch)) // Assuming this is a double league match
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => ETypeOfMatch.DoubleLeagueMatch.ToString())) // Convert enum to string
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.Id)) // Map Id to MatchId

                // Mapping Player IDs
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.TeamOne[0].UserId)) // Map first player of TeamOne as UserId
                .ForMember(dest => dest.TeamMateId, opt => opt.MapFrom(src => src.TeamOne.Length > 1 ? (int?)src.TeamOne[1].UserId : null)) // Map second player of TeamOne as TeamMateId if exists
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.TeamTwo[0].UserId)) // Map first player of TeamTwo as OpponentId
                .ForMember(dest => dest.OpponentTwoId, opt => opt.MapFrom(src => src.TeamTwo.Length > 1 ? (int?)src.TeamTwo[1].UserId : null)) // Map second player of TeamTwo as OpponentTwoId if exists

                // Mapping Player Names and Photos (TeamOne)
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.TeamOne[0].FirstName)) // Map first player of TeamOne's first name
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.TeamOne[0].LastName)) // Map first player of TeamOne's last name
                .ForMember(dest => dest.UserPhotoUrl, opt => opt.MapFrom(src => src.TeamOne[0].PhotoUrl)) // Map first player of TeamOne's photo URL
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.MapFrom(src => src.TeamOne.Length > 1 ? src.TeamOne[1].FirstName : null)) // Map second player of TeamOne's first name if exists
                .ForMember(dest => dest.TeamMateLastName, opt => opt.MapFrom(src => src.TeamOne.Length > 1 ? src.TeamOne[1].LastName : null)) // Map second player of TeamOne's last name if exists
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.MapFrom(src => src.TeamOne.Length > 1 ? src.TeamOne[1].PhotoUrl : null)) // Map second player of TeamOne's photo URL if exists

                // Mapping Opponent Names and Photos (TeamTwo)
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.TeamTwo[0].FirstName)) // Map first player of TeamTwo's first name
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.TeamTwo[0].LastName)) // Map first player of TeamTwo's last name
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.TeamTwo[0].PhotoUrl)) // Map first player of TeamTwo's photo URL
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.MapFrom(src => src.TeamTwo.Length > 1 ? src.TeamTwo[1].FirstName : null)) // Map second player of TeamTwo's first name if exists
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.MapFrom(src => src.TeamTwo.Length > 1 ? src.TeamTwo[1].LastName : null)) // Map second player of TeamTwo's last name if exists
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.MapFrom(src => src.TeamTwo.Length > 1 ? src.TeamTwo[1].PhotoUrl : null)) // Map second player of TeamTwo's photo URL if exists

                // Mapping Scores
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.TeamOneScore)) // Map TeamOneScore to UserScore
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.TeamTwoScore)) // Map TeamTwoScore to OpponentUserOrTeamScore

                // Mapping Date and League
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime ?? DateTime.MinValue)) // Map StartTime to DateOfGame
                .ForMember(dest => dest.LeagueId, opt => opt.MapFrom(src => src.LeagueId)); // Map LeagueId

            CreateMap<FreehandMatchRealTime, Match>()
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.MatchId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.PlayerOneId))
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.PlayerOne.FirstName))
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.PlayerOne.LastName))
                .ForMember(dest => dest.UserPhotoUrl, opt => opt.MapFrom(src => src.PlayerOne.PhotoUrl))
                .ForMember(dest => dest.TeamMateId, opt => opt.Ignore()) // No team mate in a single match
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.Ignore()) // No team mate in a single match
                .ForMember(dest => dest.TeamMateLastName, opt => opt.Ignore()) // No team mate in a single match
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.Ignore()) // No team mate in a single match
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.PlayerTwoId))
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.PlayerTwo.FirstName)) // Map from PlayerTwo
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.PlayerTwo.LastName)) // Map from PlayerTwo
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.PlayerTwo.PhotoUrl)) // Map from PlayerTwo
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.Ignore()) // No second opponent in a single match
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.Ignore()) // No second opponent in a single match
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.Ignore()) // No second opponent in a single match
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.PlayerOneScore))
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.PlayerTwoScore))
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.LeagueId, opt => opt.Ignore()) // Assume LeagueId is not part of FreehandMatchRealTime
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.FreehandMatch)) // Set type of match to FreehandMatch
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => "FreehandMatch"));

            CreateMap<DoubleFreehandMatchRealTime, Match>()
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.MatchId))
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.DoubleFreehandMatch)) // Assuming ETypeOfMatch.Double represents double matches
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => "DoubleFreehandMatch"))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.TeamAPlayerOneId)) // Mapping Team A Player One as User
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.TeamAPlayerOne.FirstName))
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.TeamAPlayerOne.LastName))
                .ForMember(dest => dest.UserPhotoUrl, opt => opt.MapFrom(src => src.TeamAPlayerOne.PhotoUrl))
                .ForMember(dest => dest.TeamMateId, opt => opt.MapFrom(src => src.TeamAPlayerTwoId))
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.MapFrom(src => src.TeamAPlayerTwo.FirstName))
                .ForMember(dest => dest.TeamMateLastName, opt => opt.MapFrom(src => src.TeamAPlayerTwo.LastName))
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.MapFrom(src => src.TeamAPlayerTwo.PhotoUrl))
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.TeamBPlayerOneId)) // Mapping Team B Player One as Opponent
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.TeamBPlayerOne.FirstName))
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.TeamBPlayerOne.LastName))
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.TeamBPlayerOne.PhotoUrl))
                .ForMember(dest => dest.OpponentTwoId, opt => opt.MapFrom(src => src.TeamBPlayerTwoId))
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.MapFrom(src => src.TeamBPlayerTwo.FirstName))
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.MapFrom(src => src.TeamBPlayerTwo.LastName))
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.MapFrom(src => src.TeamBPlayerTwo.PhotoUrl))
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.TeamAScore)) // Mapping Team A Score as User Score
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.TeamBScore)) // Mapping Team B Score as Opponent Score
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime)) // Mapping Start Time as Date of Game
                .ForMember(dest => dest.LeagueId, opt => opt.Ignore()); // Assuming LeagueId is not available in DoubleFreehandMatchRealTime
            
            CreateMap<SingleLeagueMatchRealTime, Match>()
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.MatchId))
                .ForMember(dest => dest.LeagueId, opt => opt.MapFrom(src => src.LeagueId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.PlayerOneId))
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.PlayerOne.FirstName))
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.PlayerOne.LastName))
                .ForMember(dest => dest.UserPhotoUrl, opt => opt.MapFrom(src => src.PlayerOne.PhotoUrl))
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.PlayerTwoId))
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.PlayerTwo.FirstName))
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.PlayerTwo.LastName))
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.PlayerTwo.PhotoUrl))
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.PlayerOneScore))
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.PlayerTwoScore))
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime ?? DateTime.Now)) // Handle null StartTime
                .ForMember(dest => dest.LastGoal, opt => opt.MapFrom(src => src.LastGoal))
                .ForMember(dest => dest.TeamMateId, opt => opt.Ignore()) // Ignore if not relevant
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.Ignore()) // Ignore if not relevant
                .ForMember(dest => dest.TeamMateLastName, opt => opt.Ignore()) // Ignore if not relevant
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.Ignore()) // Ignore if not relevant
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.SingleLeagueMatch)) // Assuming ETypeOfMatch.Double represents double matches
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => "SingleLeagueMatch"));
        
            CreateMap<DoubleLeagueMatchRealTime, Match>()
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.MatchId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.TeamOnePlayers[0].Id)) // Assuming the first player in team one is the main user
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.TeamOnePlayers[0].FirstName))
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.TeamOnePlayers[0].LastName))
                .ForMember(dest => dest.UserPhotoUrl, opt => opt.MapFrom(src => src.TeamOnePlayers[0].PhotoUrl))
                .ForMember(dest => dest.TeamMateId, opt => opt.MapFrom(src => src.TeamOnePlayers.Count > 1 ? (int?)src.TeamOnePlayers[1].Id : null)) // Handle if there's a second player
                .ForMember(dest => dest.TeamMateFirstName, opt => opt.MapFrom(src => src.TeamOnePlayers.Count > 1 ? src.TeamOnePlayers[1].FirstName : null))
                .ForMember(dest => dest.TeamMateLastName, opt => opt.MapFrom(src => src.TeamOnePlayers.Count > 1 ? src.TeamOnePlayers[1].LastName : null))
                .ForMember(dest => dest.TeamMatePhotoUrl, opt => opt.MapFrom(src => src.TeamOnePlayers.Count > 1 ? src.TeamOnePlayers[1].PhotoUrl : null))
                .ForMember(dest => dest.OpponentId, opt => opt.MapFrom(src => src.TeamTwoPlayers[0].Id))
                .ForMember(dest => dest.OpponentOneFirstName, opt => opt.MapFrom(src => src.TeamTwoPlayers[0].FirstName))
                .ForMember(dest => dest.OpponentOneLastName, opt => opt.MapFrom(src => src.TeamTwoPlayers[0].LastName))
                .ForMember(dest => dest.OpponentOnePhotoUrl, opt => opt.MapFrom(src => src.TeamTwoPlayers[0].PhotoUrl))
                .ForMember(dest => dest.OpponentTwoId, opt => opt.MapFrom(src => src.TeamTwoPlayers.Count > 1 ? (int?)src.TeamTwoPlayers[1].Id : null))
                .ForMember(dest => dest.OpponentTwoFirstName, opt => opt.MapFrom(src => src.TeamTwoPlayers.Count > 1 ? src.TeamTwoPlayers[1].FirstName : null))
                .ForMember(dest => dest.OpponentTwoLastName, opt => opt.MapFrom(src => src.TeamTwoPlayers.Count > 1 ? src.TeamTwoPlayers[1].LastName : null))
                .ForMember(dest => dest.OpponentTwoPhotoUrl, opt => opt.MapFrom(src => src.TeamTwoPlayers.Count > 1 ? src.TeamTwoPlayers[1].PhotoUrl : null))
                .ForMember(dest => dest.UserScore, opt => opt.MapFrom(src => src.TeamOneScore))
                .ForMember(dest => dest.OpponentUserOrTeamScore, opt => opt.MapFrom(src => src.TeamTwoScore))
                .ForMember(dest => dest.DateOfGame, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.LastGoal, opt => opt.MapFrom(src => src.LastGoal))
                .ForMember(dest => dest.LeagueId, opt => opt.MapFrom(src => src.LeagueId))
                .ForMember(dest => dest.TypeOfMatch, opt => opt.MapFrom(src => ETypeOfMatch.DoubleLeagueMatch)) // Assuming this is a double match
                .ForMember(dest => dest.TypeOfMatchName, opt => opt.MapFrom(src => "DoubleLeagueMatch"));
        }
    }
}
