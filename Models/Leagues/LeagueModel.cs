namespace FoosballApi.Models.Leagues
{
    public class LeagueModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public LeagueType TypeOfLeague { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UpTo { get; set; }
        public int OrganisationId { get; set; }
        public bool HasLeagueStarted { get; set; }
        public int? HowManyRounds { get; set; }
    }
}