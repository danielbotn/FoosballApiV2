using System;
using FoosballApi.Enums;
using FoosballApi.Models.Matches;

namespace FoosballApi.Models
{
    public class Match
    {
        public ETypeOfMatch TypeOfMatch { get; set; }
        public string TypeOfMatchName { get; set; }
        public int UserId { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserPhotoUrl { get; set; }
        public int? TeamMateId { get; set; }
        public int MatchId { get; set; }
        public int OpponentId { get; set; }
        public int? OpponentTwoId { get; set; }
        public string OpponentOneFirstName { get; set; }
        public string OpponentOneLastName { get; set; }
        public string OpponentOnePhotoUrl { get; set; }
        public string OpponentTwoFirstName { get; set; }
        public string OpponentTwoLastName { get; set; }
        public string OpponentTwoPhotoUrl { get; set; }
        public string TeamMateFirstName { get; set; }
        public string TeamMateLastName { get; set; }
        public string TeamMatePhotoUrl { get; set; }
        public int UserScore { get; set; }
        public int OpponentUserOrTeamScore { get; set; }
        public DateTime DateOfGame { get; set; }
        public int? LeagueId { get; set; }
        public GoalInfo LastGoal { get; set; } = null;
    }
}