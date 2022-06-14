using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.DoubleLeagueTeams;
using FoosballApi.Models.Leagues;

namespace FoosballApi.Models.DoubleLeagueMatches
{
    public class DoubleLeagueMatchModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TeamOneId { get; set; }

        [ForeignKey("TeamOneId")]
        public virtual DoubleLeagueTeamModel DoubleLeagueTeamModelTeamOneFk { get; set; }

        [Required]
        public int TeamTwoId { get; set; }

        [ForeignKey("TeamTwoId")]
        public virtual DoubleLeagueTeamModel DoubleLeagueTeamModelTeamTwoFk { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [ForeignKey("LeagueId")]
        public virtual LeagueModel LeagueModelFk { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int? TeamOneScore { get; set; }

        public int? TeamTwoScore { get; set; }

        public bool? MatchStarted { get; set; }

        public bool? MatchEnded { get; set; }

        public bool? MatchPaused { get; set; }
    }
}