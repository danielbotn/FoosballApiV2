using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueMatches;

namespace FoosballApi.Dtos.DoubleLeagueMatches
{
    public class DoubleLeagueStandingsReadDto
    {
        public int TeamID { get; set; }
        public int LeagueId { get; set; }
        public int TotalMatchesWon { get; set; }
        public int TotalMatchesLost { get; set; }
        public int TotalGoalsScored { get; set; }
        public int TotalGoalsRecieved { get; set; }
        public int PositionInLeague { get; set; }
        public int MatchesPlayed { get; set; }
        public int Points { get; set; }
        public string TeamName { get; set; }
        public TeamMember[] TeamMembers { get; set; }
    }
}