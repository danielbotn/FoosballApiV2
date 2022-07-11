namespace FoosballApi.Models.Goals
{
    public class FreehandDoubleGoalPermission
    {
        public int DoubleMatchId { get; set; }
        public int ScoredByUserId { get; set; }
        public int PlayerOneTeamA  { get; set; }
        public int PlayerTwoTeamA { get; set; }
        public int PlayerOneTeamB { get; set; }
        public int PlayerTwoTeamB { get; set; }
    }
}