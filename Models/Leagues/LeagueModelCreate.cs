using System;
using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models.Leagues
{
    public class LeagueModelCreate
    {

        [Required]
        public string Name { get; set; }

        [Required]
        public LeagueType TypeOfLeague { get; set; }

        [Required]
        public int UpTo { get; set; }

        [Required]
        public int OrganisationId { get; set; }

        public int? HowManyRounds { get; set; }

    }
}