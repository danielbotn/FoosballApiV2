using AutoMapper;
using FoosballApi.Enums;
using FoosballApi.Models;
using FoosballApi.Models.Matches;

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
        }
    }
}
