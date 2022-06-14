namespace FoosballApi.Models.DoubleLeaguePlayers
{
    public class DoubleLeaguePlayerModelDapper
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DoubleLeagueTeamId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
    }
}