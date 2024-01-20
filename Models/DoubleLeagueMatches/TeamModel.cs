namespace FoosballApi.Models.DoubleLeagueMatches
{
    public class TeamModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhotoUrl { get; set; }
        public string TeamName { get; set; }
        public int UserId { get; set; }
        public int TeamId { get; set; }
    }
}