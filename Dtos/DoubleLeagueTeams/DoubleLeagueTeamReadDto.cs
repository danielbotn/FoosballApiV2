using System;

namespace FoosballApi.Dtos.DoubleLeagueTeams
{
    public class DoubleLeagueTeamReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }

        public int OrganisationId { get; set; }

        public int LeagueId { get; set; }

    }
}