namespace FoosballApi.Models.Goals
{
    public class FreehandDoubleGoalModel
    {
        public int Id { get; set; }

        public DateTime TimeOfGoal { get; set; }

        public int DoubleMatchId { get; set; }

        public int ScoredByUserId { get; set; }

        public int ScorerTeamScore { get; set; }

        public int OpponentTeamScore { get; set; }

        public bool WinnerGoal { get; set; }
    }
}