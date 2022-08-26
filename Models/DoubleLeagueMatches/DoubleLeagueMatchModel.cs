namespace FoosballApi.Models.DoubleLeagueMatches
{
    public class DoubleLeagueMatchModel
    {
        public int Id { get; set; }
        public int TeamOneId { get; set; }
        public int TeamTwoId { get; set; }
        public int LeagueId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? TeamOneScore { get; set; }
        public int? TeamTwoScore { get; set; }
        public bool? MatchStarted { get; set; }
        public bool? MatchEnded { get; set; }
        public bool? MatchPaused { get; set; }
        
    }
}