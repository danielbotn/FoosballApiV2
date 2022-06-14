namespace FoosballApi.Models.SingleLeagueGoals
{
    public class SingleLeagueCreateModel
    {
        public int MatchId { get; set; }

        public int ScoredByUserId { get; set; }

        public int OpponentId { get; set; }

        public int ScorerScore { get; set; }

        public int OpponentScore { get; set; }

        public bool WinnerGoal { get; set; }
    }
}