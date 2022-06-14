using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.DoubleLeagueTeams;

namespace FoosballApi.Models.DoubleLeaguePlayers
{
    public class DoubleLeaguePlayerModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User UserIdFk { get; set; }

        [Required]
        public int DoubleLeagueTeamId { get; set; }

        [ForeignKey("DoubleLeagueTeamId")]
        public virtual DoubleLeagueTeamModel DoubleLeagueTeamModelFk { get; set; }
    }
}