using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Models.Leagues
{
    public class LeaguePlayersJoinModel
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [Required]
        public DateTime Created_at { get; set; }

        [Required]

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}