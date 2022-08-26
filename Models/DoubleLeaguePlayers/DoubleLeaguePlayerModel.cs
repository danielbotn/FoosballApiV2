using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models.DoubleLeaguePlayers
{
    public class DoubleLeaguePlayerModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int DoubleLeagueTeamId { get; set; }

    }
}