namespace FoosballApi.Models.Goals
{
    public class FreehandGoalPermission
    {
        public int MatchId { get; set; }
        public int ScoredByUserId { get; set; }
        public int PlayerOneId { get; set; }
        public int? PlayerTwoId { get; set; }
    }
}