namespace FoosballApi.Dtos.SingleLeagueMatches
{
    public class SingleLeagueStandingsReadDto
    {
        public int userId { get; set; }
        public int leagueId { get; set; }
        public int totalMatchesWon { get; set; }
        public int totalMatchesLost { get; set; }
        public int totalGoalsScored { get; set; }
        public int TotalGoalsRecieved { get; set; }
        public int positionInLeague { get; set; }
        public int Points { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}