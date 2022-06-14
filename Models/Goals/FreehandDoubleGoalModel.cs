using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.Matches;

namespace FoosballApi.Models.Goals
{
    public class FreehandDoubleGoalModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime TimeOfGoal { get; set; }

        public int DoubleMatchId { get; set; }

        [ForeignKey("DoubleMatchId")]
        public virtual FreehandDoubleMatchModel freehandDoubleMatchModel { get; set; }

        public int ScoredByUserId { get; set; }

        [ForeignKey("ScoredByUserId")]
        public virtual User userScoredByUserId { get; set; }

        public int ScorerTeamScore { get; set; }

        public int OpponentTeamScore { get; set; }

        public bool WinnerGoal { get; set; }
    }
}