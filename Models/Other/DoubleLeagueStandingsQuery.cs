using FoosballApi.Models.DoubleLeagueMatches;

namespace FoosballApi.Models.Other
{
    public class DoubleLeagueStandingsQuery
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
        public TeamMember[] TeamMembers { get; set; }
    }
}