using System;

namespace FoosballApi.Dtos.Goals
{
    public class FreehandGoalUpdateDto
    {
        public DateTime TimeOfGoal { get; set; }

        public int MatchId { get; set; }

        public int ScoredByUserId { get; set; }

        public int OponentId { get; set; }

        public int ScoredByScore { get; set; }

        public int OponentScore { get; set; }

        public bool WinnerGoal { get; set; }
    }
}