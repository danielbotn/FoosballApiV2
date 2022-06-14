using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.Leagues;

namespace FoosballApi.Models.Matches
{
    public class SingleLeagueMatchModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int PlayerOne { get; set; }

        [ForeignKey("PlayerOne")]
        public virtual User UserPlayerOne { get; set; }

        [Required]
        public int PlayerTwo { get; set; }

        [ForeignKey("PlayerTwo")]
        public virtual User UserPlayerTwo { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [ForeignKey("LeagueId")]
        public virtual LeagueModel League { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int? PlayerOneScore { get; set; }

        public int? PlayerTwoScore { get; set; }

        public bool? MatchStarted { get; set; }

        public bool? MatchEnded { get; set; }

        public bool? MatchPaused { get; set; }

    }
}