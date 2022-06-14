using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Dtos.DoubleGoals
{
    public class FreehandDoubleGoalCreateDto
    {
        [Required]
        public int DoubleMatchId { get; set; }
        [Required]
        public int ScoredByUserId { get; set; }
        [Required]
        public int ScorerTeamScore { get; set; }
        [Required]
        public int OpponentTeamScore { get; set; }

        public bool WinnerGoal { get; set; }
    }
}