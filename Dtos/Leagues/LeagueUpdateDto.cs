using FoosballApi.Models.Leagues;

namespace FoosballApi.Dtos.Leagues
{
    public class LeagueUpdateDto
    {

        public string Name { get; set; }

        public TypeOfLeague TypeOfLeague { get; set; }

        public int UpTo { get; set; }

        public bool HasLeagueStarted { get; set; }

    }
}