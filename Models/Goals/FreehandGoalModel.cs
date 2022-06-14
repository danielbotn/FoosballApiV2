using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.Matches;

namespace FoosballApi.Models.Goals
{
    public class FreehandGoalModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime TimeOfGoal { get; set; }

        [Required]
        public int MatchId { get; set; }

        [ForeignKey("MatchId")]
        public virtual FreehandMatchModel freehandMatchModel { get; set; }

        [Required]
        public int ScoredByUserId { get; set; }

        [ForeignKey("ScoredByUserId")]
        public virtual User userScoredByUserId { get; set; }

        [Required]
        public int OponentId { get; set; }

        [ForeignKey("OponentId")]
        public virtual User userOponentId { get; set; }

        [Required]
        public int ScoredByScore { get; set; }

        [Required]
        public int OponentScore { get; set; }

        [Required]
        public bool WinnerGoal { get; set; }
    }
}