namespace FoosballApi.Models.DoubleLeagueTeams
{
    public class DoubleLeagueTeamsJoinModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DoubleLeagueTeamId { get; set; }
        public int LeagueId { get; set; }
    }
}
