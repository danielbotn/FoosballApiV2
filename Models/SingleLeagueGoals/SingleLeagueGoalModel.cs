using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.Matches;

namespace FoosballApi.Models.SingleLeagueGoals
{
    public class SingleLeagueGoalModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime TimeOfGoal { get; set; }

        [Required]
        public int MatchId { get; set; }

        [ForeignKey("MatchId")]
        public virtual SingleLeagueMatchModel SingleLeagueMatchId { get; set; }

        [Required]
        public int ScoredByUserId { get; set; }

        [ForeignKey("ScoredByUserId")]
        public virtual User ScoredByUserIdUser { get; set; }

        [Required]
        public int OpponentId { get; set; }

        [ForeignKey("OpponentId")]
        public virtual User OpponentIdUser { get; set; }

        [Required]
        public int ScorerScore { get; set; }

        [Required]
        public int OpponentScore { get; set; }

        [Required]
        public bool WinnerGoal { get; set; }

    }
}