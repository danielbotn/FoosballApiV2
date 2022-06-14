using System;
using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Dtos.DoubleMatches
{
    public class FreehandDoubleMatchCreateDto
    {
        [Required]
        public int PlayerOneTeamA { get; set; }

        public int? PlayerTwoTeamA { get; set; }

        [Required]
        public int PlayerOneTeamB { get; set; }

        public int? PlayerTwoTeamB { get; set; }

        [Required]
        public int OrganisationId { get; set; }

        public int? TeamAScore { get; set; }
        public int? TeamBScore { get; set; }
        public string NicknameTeamA { get; set; }
        public string NicknameTeamB { get; set; }
        public int? UpTo { get; set; }
    }
}