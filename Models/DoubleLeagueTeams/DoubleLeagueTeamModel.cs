using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models.Leagues;

namespace FoosballApi.Models.DoubleLeagueTeams
{
    public class DoubleLeagueTeamModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int OrganisationId { get; set; }

        public int LeagueId { get; set; }
    }
}