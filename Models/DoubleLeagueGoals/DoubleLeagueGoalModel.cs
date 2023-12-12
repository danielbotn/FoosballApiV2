namespace FoosballApi.Models.DoubleLeagueGoals
{
    public class DoubleLeagueGoalModel
    {
        public int Id { get; set; }
        public DateTime TimeOfGoal { get; set; }
        public int MatchId { get; set; }
        public int ScoredByTeamId { get; set; }
        public int OpponentTeamId { get; set; }
        public int ScorerTeamScore { get; set; }
        public int OpponentTeamScore { get; set; }
        public bool WinnerGoal { get; set; }
        public int UserScorerId { get; set; }
    }
}