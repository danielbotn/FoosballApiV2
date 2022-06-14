using System;

namespace FoosballApi.Dtos.DoubleLeagueMatches
{
    public class DoubleLeagueMatchUpdateDto
    {
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int? TeamOneScore { get; set; }

        public int? TeamTwoScore { get; set; }

        public bool? MatchStarted { get; set; }

        public bool? MatchEnded { get; set; }

        public bool? MatchPaused { get; set; }
    }
}