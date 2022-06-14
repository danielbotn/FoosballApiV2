using System;
using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Dtos.DoubleLeagueGoals
{
    public class DoubleLeagueGoalCreateDto
    {
        [Required]
        public DateTime TimeOfGoal { get; set; }
        [Required]
        public int MatchId { get; set; }
        [Required]
        public int ScoredByTeamId { get; set; }
        [Required]
        public int OpponentTeamId { get; set; }
        [Required]
        public int ScorerTeamScore { get; set; }
        [Required]
        public int OpponentTeamScore { get; set; }
        public bool? WinnerGoal { get; set; }
        [Required]
        public int UserScorerId { get; set; }
    }
}