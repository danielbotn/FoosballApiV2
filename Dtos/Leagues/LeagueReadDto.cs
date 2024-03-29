using System;
using System.ComponentModel.DataAnnotations;
using FoosballApi.Models.Leagues;

namespace FoosballApi.Dtos.Leagues
{
    public class LeagueReadDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public TypeOfLeague TypeOfLeague { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int UpTo { get; set; }

        [Required]
        public int OrganisationId { get; set; }

        [Required]
        public bool HasLeagueStarted { get; set; }
        
        [Required]
        public int? HowManyRounds { get; set; }
    }
}