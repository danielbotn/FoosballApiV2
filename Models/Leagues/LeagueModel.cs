using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Models.Leagues
{
    public class LeagueModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public LeagueType TypeOfLeague { get; set; }

        [Required]
        public DateTime Created_at { get; set; }

        [Required]
        public int UpTo { get; set; }

        [Required]
        public int OrganisationId { get; set; }

        [ForeignKey("OrganisationId")]
        public virtual OrganisationModel OrganisationModel { get; set; }

        public bool HasLeagueStarted { get; set; }

        public int? HowManyRounds { get; set; }
    }
}