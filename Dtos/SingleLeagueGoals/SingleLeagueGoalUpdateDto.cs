using System;

namespace FoosballApi.Dtos.SingleLeagueGoals
{
    public class SingleLeagueGoalUpdateDto
    {
        public DateTime TimeOfGoal { get; set; }

        public int ScoredByUserId { get; set; }

        public int OpponentId { get; set; }

        public int ScorerScore { get; set; }

        public int OpponentScore { get; set; }

        public bool WinnerGoal { get; set; }
    }
}