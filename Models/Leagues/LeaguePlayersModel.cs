using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models.Leagues
{
    public class LeaguePlayersModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}