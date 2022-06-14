using System;

namespace FoosballApi.Models.Matches
{
    public class SingleLeagueMathModelDapper
    {
        public int Id { get; set; }
        public int PlayerOne { get; set; }
        public int PlayerTwo { get; set; }
        public int LeagueId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int PlayerOneScore { get; set; }

        public int PlayerTwoScore { get; set; }

        public bool MatchStarted { get; set; }

        public bool MatchEnded { get; set; }

        public bool MatchPaused { get; set; }

    }
}
