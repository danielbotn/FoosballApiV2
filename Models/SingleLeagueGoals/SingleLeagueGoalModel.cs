using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models.SingleLeagueGoals
{
    public class SingleLeagueGoalModel
    {
        public int Id { get; set; }
        [Required]
        public DateTime TimeOfGoal { get; set; }
        [Required]
        public int MatchId { get; set; }
        [Required]
        public int ScoredByUserId { get; set; }
        [Required]
        public int OpponentId { get; set; }
        [Required]
        public int ScorerScore { get; set; }
        [Required]
        public int OpponentScore { get; set; }
        [Required]
        public bool WinnerGoal { get; set; }

    }
}