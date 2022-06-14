using System;

namespace FoosballApi.Models.Matches
{
    public class FreehandDoubleMatchModelExtended
    {
        public int Id { get; set; }
        public int PlayerOneTeamA { get; set; }
        public string PlayerOneTeamAFirstName { get; set; }
        public string PlayerOneTeamALastName { get; set; }
        public string PlayerOneTeamAPhotoUrl { get; set; }
        public int? PlayerTwoTeamA { get; set; }
        public string PlayerTwoTeamAFirstName { get; set; }
        public string PlayerTwoTeamALastName { get; set; }
        public string PlayerTwoTeamAPhotoUrl { get; set; }
        public int PlayerOneTeamB { get; set; }
        public string PlayerOneTeamBFirstName { get; set; }
        public string PlayerOneTeamBLastName { get; set; }
        public string PlayerOneTeamBPhotoUrl { get; set; }
        public int? PlayerTwoTeamB { get; set; }
        public string PlayerTwoTeamBFirstName { get; set; }
        public string PlayerTwoTeamBLastName { get; set; }
        public string PlayerTwoTeamBPhotoUrl { get; set; }
        public int OrganisationId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string TotalPlayingTime { get; set; }
        public int? TeamAScore { get; set; }
        public int? TeamBScore { get; set; }
        public string NicknameTeamA { get; set; }
        public string NicknameTeamB { get; set; }
        public int? UpTo { get; set; }
        public bool? GameFinished { get; set; }
        public bool? GamePaused { get; set; }
    }
}