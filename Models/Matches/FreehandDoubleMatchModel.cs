using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Models.Matches
{
    public class FreehandDoubleMatchModel
    {
        public int Id { get; set; }
        public int PlayerOneTeamA { get; set; }
        public int? PlayerTwoTeamA { get; set; }
        public int PlayerOneTeamB { get; set; }
        public int? PlayerTwoTeamB { get; set; }
        public int OrganisationId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? TeamAScore { get; set; }
        public int? TeamBScore { get; set; }
        public string NicknameTeamA { get; set; }
        public string NicknameTeamB { get; set; }
        public int? UpTo { get; set; }
        public bool? GameFinished { get; set; }
        public bool? GamePaused { get; set; }
    }
}