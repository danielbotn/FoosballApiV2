using System.ComponentModel.DataAnnotations;
using FoosballApi.Models;

namespace FoosballApi.Dtos.Goals
{
    public class FreehandGoalCreateDto
    {
        [Required]
        public int MatchId { get; set; }
        [Required]
        public int ScoredByUserId { get; set; }
        [Required]
        public int OponentId { get; set; }
        [Required]
        public int ScoredByScore { get; set; }
        [Required]
        public int OponentScore { get; set; }

        [Required]
        public bool WinnerGoal { get; set; }
    }
}