using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.DoubleLeagueTeams;

namespace FoosballApi.Models.DoubleLeagueGoals
{
    public class DoubleLeagueGoalModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime TimeOfGoal { get; set; }

        [Required]
        public int MatchId { get; set; }

        [ForeignKey("MatchId")]
        public virtual DoubleLeagueMatchModel DoubleLeagueMatchModelFk { get; set; }

        [Required]
        public int ScoredByTeamId { get; set; }

        [ForeignKey("ScoredByTeamId")]
        public virtual DoubleLeagueTeamModel DoubleLeagueTeamModelScoredByFk { get; set; }

        [Required]
        public int OpponentTeamId { get; set; }

        [ForeignKey("OpponentTeamId")]
        public virtual DoubleLeagueTeamModel DoubleLeagueTeamModelOpponentFk { get; set; }

        [Required]
        public int ScorerTeamScore { get; set; }

        [Required]
        public int OpponentTeamScore { get; set; }

        [Required]
        public bool WinnerGoal { get; set; }

        [Required]
        public int UserScorerId { get; set; }

        [ForeignKey("UserScorerId")]
        public virtual User UserScorerIdFk { get; set; }
    }
}