using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.Leagues;

namespace FoosballApi.Models.DoubleLeagueTeams
{
    public class DoubleLeagueTeamModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime? CreatedAt { get; set; }

        [Required]
        public int OrganisationId { get; set; }

        [ForeignKey("OrganisationId")]
        public virtual OrganisationModel OrganisationModelFk { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [ForeignKey("LeagueId")]
        public virtual LeagueModel LeagueModelFk { get; set; }
    }
}