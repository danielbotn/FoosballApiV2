using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Models.Matches
{
    public class FreehandDoubleMatchModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int PlayerOneTeamA { get; set; }

        [ForeignKey("PlayerOneTeamA")]
        public virtual User UserPlayerOneTeamA { get; set; }

        public int? PlayerTwoTeamA { get; set; }

        [ForeignKey("PlayerTwoTeamA")]
        public virtual User UserPlayerTwoTeamA { get; set; }

        [Required]
        public int PlayerOneTeamB { get; set; }

        [ForeignKey("PlayerOneTeamB")]
        public virtual User UserPlayerOneTeamB { get; set; }

        public int? PlayerTwoTeamB { get; set; }

        [ForeignKey("PlayerTwoTeamB")]
        public virtual User UserPlayerTwoTeamB { get; set; }

        [Required]
        public int OrganisationId { get; set; }

        [ForeignKey("OrganisationId")]
        public virtual OrganisationModel OrganisationModel { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? TeamAScore { get; set; }
        public int? TeamBScore { get; set; }
        public string NicknameTeamA { get; set; }
        public string NicknameTeamB { get; set; }
        public int? UpTo { get; set; }
        public bool? GameFinished { get; set; }
        public bool? GamePaused { get; set; }
    }
}