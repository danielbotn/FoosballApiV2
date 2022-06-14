namespace FoosballApi.Dtos.Users
{
    public class UserStatsReadDto
    {
        public int UserId { get; set; }
        public int TotalMatches { get; set; }
        public int TotalFreehandMatches { get; set; }
        public int TotalDoubleFreehandMatches { get; set; }
        public int TotalSingleLeagueMatches { get; set; }
        public int TotalDoubleLeagueMatches { get; set; }
        public int TotalMatchesWon { get; set; }
        public int TotalMatchesLost { get; set; }
        public int TotalGoalsScored { get; set; }
        public int TotalGoalsReceived { get; set; }
    }
}