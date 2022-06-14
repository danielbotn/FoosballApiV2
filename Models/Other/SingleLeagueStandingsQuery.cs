namespace FoosballApi.Models.Other
{
    public class SingleLeagueStandingsQuery
    {
        public SingleLeagueStandingsQuery(
            int userId,
            int leagueId,
            int totalMatchesWon,
            int totalMatchesLost,
            int totalGoalsScored,
            int totalGoalsRecieved,
            int positionInLeague,
            int matchesPlayed,
            int points,
            string firstName,
            string lastName,
            string email
        )
        {
            this.UserId = userId;
            this.LeagueId = leagueId;
            this.TotalMatchesWon = totalMatchesWon;
            this.TotalMatchesLost = totalMatchesLost;
            this.TotalGoalsScored = totalGoalsScored;
            this.TotalGoalsRecieved = totalGoalsRecieved;
            this.PositionInLeague = positionInLeague;
            this.MatchesPlayed = matchesPlayed;
            this.Points = points;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.Email = email;
        }
        public int UserId { get; set; }
        public int LeagueId { get; set; }
        public int TotalMatchesWon { get; set; }
        public int TotalMatchesLost { get; set; }
        public int TotalGoalsScored { get; set; }
        public int TotalGoalsRecieved { get; set; }
        public int PositionInLeague { get; set; }
        public int MatchesPlayed { get; set; }
        public int Points { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}