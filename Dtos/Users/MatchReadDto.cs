using System;
using FoosballApi.Enums;

namespace FoosballApi.Dtos.Users
{
    public class MatchReadDto
    {
        public ETypeOfMatch TypeOfMatch { get; set; }
        public string TypeOfMatchName { get; set; }
        public int UserId { get; set; }
        public int? TeamMateId { get; set; }

        public string TeamMateFirstName { get; set; }
        public string TeamMateLastName { get; set; }

        public string TeamMatePhotoUrl { get; set; }

        public int MatchId { get; set; }

        public int OpponentId { get; set; }

        public int? OpponentTwoId { get; set; }

        public string OpponentOneFirstName { get; set; }

        public string OpponentOneLastName { get; set; }

        public string OpponentOnePhotoUrl { get; set; }

        public string OpponentTwoFirstName { get; set; }
        
        public string OpponentTwoLastName { get; set; }

        public string OpponentTwoPhotoUrl { get; set; }

        public int UserScore { get; set; }

        public int OpponentUserOrTeamScore { get; set; }

        public DateTime DateOfGame { get; set; }

        public int? LeagueId { get; set; }

    }
}