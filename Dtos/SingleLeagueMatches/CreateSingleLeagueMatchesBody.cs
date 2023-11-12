namespace FoosballApi.Dtos.SingleLeagueMatches
{
    public class CreateSingleLeagueMatchesBody
    {
        public int LeagueId { get; set; }
        public int? HowManyRounds { get; set; }
    }
}