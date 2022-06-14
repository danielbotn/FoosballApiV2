using System;

namespace FoosballApi.Dtos.SingleLeagueMatches
{
    public class SingleLeagueMatchUpdateDto
    {
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int? PlayerOneScore { get; set; }

        public int? PlayerTwoScore { get; set; }

        public bool? MatchStarted { get; set; }

        public bool? MatchEnded { get; set; }

        public bool? MatchPaused { get; set; }
    }
}