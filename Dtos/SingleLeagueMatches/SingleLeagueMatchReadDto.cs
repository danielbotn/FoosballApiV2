using System;

namespace FoosballApi.Dtos.SingleLeagueMatches
{
    public class SingleLeagueMatchReadDto
    {
        public int Id { get; set; }
        public int PlayerOne { get; set; }
        public string PlayerOneFirstName { get; set; }
        public string PlayerOneLastName { get; set; }
        public string PlayerOnePhotoUrl { get; set; }
        public int PlayerTwo { get; set; }
        public string PlayerTwoFirstName { get; set; }
        public string PlayerTwoLastName { get; set; }
        public string PlayerTwoPhotoUrl { get; set; }
        public int LeagueId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? PlayerOneScore { get; set; }
        public int? PlayerTwoScore { get; set; }
        public bool? MatchStarted { get; set; }
        public bool? MatchEnded { get; set; }
        public bool? MatchPaused { get; set; }
        public string TotalPlayingTime { get; set; }
    }
}
