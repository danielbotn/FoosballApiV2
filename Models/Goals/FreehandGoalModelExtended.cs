using System;

namespace FoosballApi.Models.Goals
{
    public class FreehandGoalModelExtended
    {
        public int Id { get; set; }
        public DateTime TimeOfGoal { get; set; }
        public string GoalTimeStopWatch { get; set; }
        public int MatchId { get; set; }
        public int ScoredByUserId { get; set; }
        public string ScoredByUserFirstName { get; set; }
        public string ScoredByUserLastName { get; set; }
        public string ScoredByUserPhotoUrl { get; set; }
        public int OponentId { get; set; }
        public string OponentFirstName { get; set; }
        public string OponentLastName { get; set; }
        public string OponentPhotoUrl { get; set; }
        public int ScoredByScore { get; set; }
        public int OponentScore { get; set; }
        public bool WinnerGoal { get; set; }
    }
}