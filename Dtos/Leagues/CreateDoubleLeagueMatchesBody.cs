using FoosballApi.Models.DoubleLeagueMatches;

namespace FoosballApi.Dtos.Leagues
{
    public class CreateDoubleLeagueMatchesBody
    {
        public int LeagueId { get; set; }
        public int? HowManyRounds { get; set; }
    }
}