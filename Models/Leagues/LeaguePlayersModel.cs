using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Models.Leagues
{
    public class LeaguePlayersModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [ForeignKey("LeagueId")]
        public virtual LeagueModel League { get; set; }

        [Required]
        public DateTime Created_at { get; set; }
    }
}