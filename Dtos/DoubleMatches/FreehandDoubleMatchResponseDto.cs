using System;

namespace FoosballApi.Dtos.DoubleMatches
{
    public class FreehandDoubleMatchResponseDto
    {
        public int Id { get; set; }
        public int PlayerOneTeamA { get; set; }
        public int PlayerOneTeamB { get; set; }
        public int PlayerTwoTeamA { get; set; }
        public int PlayerTwoTeamB { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int UpTo { get; set; }
        public bool GameFinished { get; set; }
        public bool GamePaused { get; set; }
        public int OrganisationId { get; set; }
        public string NicknameTeamA { get; set; }
        public string NicknameTeamB { get; set; }
    }
}