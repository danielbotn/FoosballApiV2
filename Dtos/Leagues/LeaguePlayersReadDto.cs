using System;
using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Dtos.Leagues
{
    public class LeaguePlayersReadDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [Required]
        public DateTime Created_at { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}